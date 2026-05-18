<?php
require_once __DIR__ . '/BaseController.php';
require_once __DIR__ . '/../Models/Models.php';
require_once __DIR__ . '/../Repositories/CourseAndSubmissionRepository.php';

// ============================================================
// SubmissionController (RF-09, RF-10, RF-12)
// ============================================================

/**
 * SubmissionController — Gestiona entregas individuales y grupales.
 *
 * Endpoints:
 *   POST /api/submissions              → submitActivity   (student)
 *   GET  /api/activities/{id}/submissions → getSubmissions (teacher)
 *   GET  /api/submissions/{id}         → getDetail        (teacher)
 */
class SubmissionController extends BaseController
{
    private SubmissionRepository $submissionRepo;

    public function __construct()
    {
        $this->submissionRepo = new SubmissionRepository();
    }

    private function getPdo(): \PDO
    {
        return \DatabaseConnection::getInstance()->getPdo();
    }

    // =========================================================
    // POST /api/submissions (RF-09, RF-12)
    // Body: { "activity_id", "group_id"(opcional), "files": [{"file_name","file_content_base64"}] }
    // =========================================================
    public function submitActivity(): void
    {
        $payload = $this->requireStudent();
        $body    = $this->getBody();

        if (empty($body['activity_id']) || empty($body['files'])) {
            $this->error('activity_id y files son obligatorios');
        }

        $activityId = (int) $body['activity_id'];
        $groupId    = !empty($body['group_id']) ? (int) $body['group_id'] : null;
        $isGroup    = $groupId !== null;

        // Verificar que la actividad existe
        $stmt = $this->getPdo()->prepare(
            "SELECT id FROM activity WHERE id = :id LIMIT 1"
        );
        $stmt->execute([':id' => $activityId]);
        if (!$stmt->fetch()) {
            $this->error('Actividad no encontrada', 404);
        }

        // Crear la submission
        $submission = new Submission(
            null,
            $activityId,
            $isGroup ? null : $payload['userId'],
            $groupId,
            $isGroup
        );

        $saved = $this->submissionRepo->save($submission);

        // Guardar cada archivo y generar firma digital
        $signatureController = new SignatureController();
        foreach ($body['files'] as $file) {
            if (empty($file['file_name']) || empty($file['file_content_base64'])) continue;

            // Decodificar contenido y guardar en disco
            $content  = base64_decode($file['file_content_base64']);
            $filePath = $this->saveFile($saved->getId(), $file['file_name'], $content);

            // Generar firma digital del archivo
            $signature = $signatureController->generateSignature($content);

            $stmt = $this->getPdo()->prepare("
                INSERT INTO submission_file (submission_id, file_name, file_path, digital_signature)
                VALUES (:sub_id, :file_name, :file_path, :signature)
            ");
            $stmt->execute([
                ':sub_id'    => $saved->getId(),
                ':file_name' => $file['file_name'],
                ':file_path' => $filePath,
                ':signature' => $signature,
            ]);
        }

        $this->success([
            'message'      => 'Actividad entregada correctamente',
            'submission_id'=> $saved->getId(),
            'submitted_at' => $saved->getSubmittedAt(),
        ], 201);
    }

    // =========================================================
    // GET /api/activities/{activityId}/submissions (RF-10)
    // =========================================================
    public function getSubmissions(int $activityId): void
    {
        $this->requireTeacher();

        $submissions = $this->submissionRepo->findByActivity($activityId);

        $result = array_map(function ($sub) {
            $data  = $sub->toArray();
            $stmt  = $this->getPdo()->prepare(
                "SELECT id, file_name, file_path, digital_signature FROM submission_file WHERE submission_id = :sid"
            );
            $stmt->execute([':sid' => $sub->getId()]);
            $data['files'] = $stmt->fetchAll();
            return $data;
        }, $submissions);

        $this->success($result);
    }

    // =========================================================
    // GET /api/submissions/{id} (RF-10)
    // =========================================================
    public function getDetail(int $submissionId): void
    {
        $this->requireTeacher();

        $submission = $this->submissionRepo->findByIdAsModel($submissionId);
        if (!$submission) {
            $this->error('Entrega no encontrada', 404);
        }

        // Verificar firma al momento de visualizar (RF-19)
        $signatureController = new SignatureController();
        $signatureController->verifySubmission($submissionId);

        // Recargar con estado actualizado
        $submission = $this->submissionRepo->findByIdAsModel($submissionId);

        $data  = $submission->toArray();
        $stmt  = $this->getPdo()->prepare(
            "SELECT id, file_name, file_path, digital_signature FROM submission_file WHERE submission_id = :sid"
        );
        $stmt->execute([':sid' => $submissionId]);
        $data['files'] = $stmt->fetchAll();

        $this->success($data);
    }

    // =========================================================
    // Helper: guarda el archivo .py en disco
    // =========================================================
    private function saveFile(int $submissionId, string $fileName, string $content): string
    {
        $dir = __DIR__ . "/../storage/submissions/{$submissionId}/";
        if (!is_dir($dir)) mkdir($dir, 0755, true);

        $safeName = basename($fileName);
        $path     = $dir . $safeName;
        file_put_contents($path, $content);

        return "storage/submissions/{$submissionId}/{$safeName}";
    }
}

// ============================================================
// SignatureController (RF-18, RF-19)
// ============================================================

/**
 * SignatureController — Firma y verifica la integridad de archivos entregados.
 *
 * No expone endpoints propios; es usado internamente por SubmissionController.
 */
class SignatureController extends BaseController
{
    // Clave privada del servidor para firmar archivos
    // En producción debe venir de una variable de entorno o archivo seguro
    private string $privateKey = 'PYSTUDIO_SIGNATURE_KEY_2026';

    /**
     * Genera una firma digital del contenido de un archivo.
     * Usa HMAC-SHA256 con la clave privada del servidor (RF-18).
     */
    public function generateSignature(string $fileContent): string
    {
        return hash_hmac('sha256', $fileContent, $this->privateKey);
    }

    /**
     * Verifica la integridad de todos los archivos de una entrega.
     * Si algún hash no coincide, marca la entrega como inválida (RF-19).
     */
    public function verifySubmission(int $submissionId): bool
    {
        $pdo  = \DatabaseConnection::getInstance()->getPdo();

        // Obtener todos los archivos de la entrega
        $stmt = $pdo->prepare(
            "SELECT * FROM submission_file WHERE submission_id = :sid"
        );
        $stmt->execute([':sid' => $submissionId]);
        $files = $stmt->fetchAll();

        $allValid = true;

        foreach ($files as $file) {
            if (empty($file['digital_signature'])) continue;

            $storedPath = __DIR__ . '/../' . $file['file_path'];

            if (!file_exists($storedPath)) {
                $allValid = false;
                continue;
            }

            $currentContent   = file_get_contents($storedPath);
            $recalculatedHash = $this->generateSignature($currentContent);

            if (!hash_equals($recalculatedHash, $file['digital_signature'])) {
                $allValid = false;
                break;
            }
        }

        // Actualizar signature_valid en la tabla submission (RF-19)
        $stmt = $pdo->prepare(
            "UPDATE submission SET signature_valid = :valid WHERE id = :id"
        );
        $stmt->execute([
            ':valid' => $allValid ? 1 : 0,
            ':id'    => $submissionId,
        ]);

        return $allValid;
    }
}
