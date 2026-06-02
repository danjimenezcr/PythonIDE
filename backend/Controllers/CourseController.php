<?php
require_once __DIR__ . '/BaseController.php';
require_once __DIR__ . '/../Models/Course.php';
require_once __DIR__ . '/../Models/Models.php';
require_once __DIR__ . '/../Repositories/CourseAndSubmissionRepository.php';

/**
 * CourseController — Gestiona cursos e inscripciones (RF-03, RF-04, RF-13, RF-17).
 * Endpoints:
 *   POST   /api/courses - createCourse (teacher)
 *   GET    /api/courses - getCourses (any auth)
 *   POST   /api/courses/enroll -enrollStudent (student)
 *   PUT    /api/courses/{id} - updateCourse (teacher)
 *   DELETE /api/courses/{id} - deleteCourse (teacher)
 *   GET    /api/courses/{id}/members - getMembers (teacher)
 *   DELETE /api/courses/{id}/members/{studentId} - removeMember (teacher)
 */
class CourseController extends BaseController
{
    private CourseRepository $courseRepo;

    public function __construct()
    {
        $this->courseRepo = new CourseRepository();
    }

    // POST /api/courses (RF-03)
    // Body: { "name", "description" }
    public function createCourse(): void
    {
        $payload = $this->requireTeacher();
        $body    = $this->getBody();

        if (empty($body['name'])) {
            $this->error('El nombre del curso es obligatorio');
        }

        // Generar access_code único
        $accessCode = $this->generateAccessCode();

        $course = new Course(
            null,
            $payload['userId'],
            $body['name'],
            $body['description'] ?? null,
            $accessCode
        );

        $saved = $this->courseRepo->save($course);
        $this->success($saved->toArray(), 201);
    }

    // GET /api/courses/{id}
    public function getCourse(int $courseId): void
    {
        $this->requireAuth();

        $course = $this->courseRepo->findByIdAsModel($courseId);
        if (!$course) {
            $this->error('Curso no encontrado', 404);
        }

        $this->success($course->toArray());
    }

    // GET /api/courses (RF-03, RF-04)
    // Devuelve cursos según el rol del usuario autenticado
    public function getCourses(): void
    {
        $payload = $this->requireAuth();

        if ($payload['role'] === 'teacher') {
            $courses = $this->courseRepo->findByTeacher($payload['userId']);
        } else {
            $courses = $this->courseRepo->findByStudent($payload['userId']);
        }

        $this->success(array_map(fn($c) => $c->toArray(), $courses));
    }

    // POST /api/courses/enroll (RF-04)
    // Body: { "access_code" }
    public function enrollStudent(): void
    {
        $payload = $this->requireStudent();
        $body    = $this->getBody();

        if (empty($body['access_code'])) {
            $this->error('El código de acceso es obligatorio');
        }

        $course = $this->courseRepo->findByAccessCode($body['access_code']);

        if (!$course) {
            $this->error('Código de acceso inválido');
        }

        // Verificar si ya está inscrito
        $pdo  = \DatabaseConnection::getInstance()->getPdo();
        $stmt = $pdo->prepare(
            "SELECT id FROM enrollment WHERE student_id = :sid AND course_id = :cid"
        );
        $stmt->execute([':sid' => $payload['userId'], ':cid' => $course->getId()]);

        if ($stmt->fetch()) {
            $this->error('Ya estás inscrito en este curso');
        }

        // Inscribir al estudiante
        $stmt = $pdo->prepare(
            "INSERT INTO enrollment (student_id, course_id) VALUES (:sid, :cid)"
        );
        $stmt->execute([':sid' => $payload['userId'], ':cid' => $course->getId()]);

        $this->success([
            'message'     => 'Inscripción exitosa',
            'course_name' => $course->getName(),
            'course_id'   => $course->getId(),
        ]);
    }

    // PUT /api/courses/{id} (RF-17)
    // Body: { "name", "description" }
    public function updateCourse(int $courseId): void
    {
        $payload = $this->requireTeacher();
        $body    = $this->getBody();

        $course = $this->courseRepo->findByIdAsModel($courseId);

        if (!$course) {
            $this->error('Curso no encontrado', 404);
        }

        if ($course->getTeacherId() !== $payload['userId']) {
            $this->error('No tienes permisos para editar este curso', 403);
        }

        if (!empty($body['name']))        $course->setName($body['name']);
        if (isset($body['description']))  $course->setDescription($body['description']);

        $updated = $this->courseRepo->save($course);
        $this->success($updated->toArray());
    }

    // DELETE /api/courses/{id} (RF-17)
    public function deleteCourse(int $courseId): void
    {
        $payload = $this->requireTeacher();

        $course = $this->courseRepo->findByIdAsModel($courseId);

        if (!$course) {
            $this->error('Curso no encontrado', 404);
        }

        if ($course->getTeacherId() !== $payload['userId']) {
            $this->error('No tienes permisos para eliminar este curso', 403);
        }

        $this->courseRepo->deleteById($courseId);
        $this->success(['message' => 'Curso eliminado correctamente']);
    }

    // GET /api/courses/{id}/members (RF-13)
    public function getMembers(int $courseId): void
    {
        $payload = $this->requireTeacher();

        $course = $this->courseRepo->findByIdAsModel($courseId);
        if (!$course) {
            $this->error('Curso no encontrado', 404);
        }
        if ($course->getTeacherId() !== $payload['userId']) {
            $this->error('No tienes permisos sobre este curso', 403);
        }

        $pdo  = \DatabaseConnection::getInstance()->getPdo();
        $stmt = $pdo->prepare("
            SELECT u.id, u.full_name, u.email, e.enrolled_at
              FROM user u
              JOIN enrollment e ON e.student_id = u.id
             WHERE e.course_id = :cid
             ORDER BY u.full_name
        ");
        $stmt->execute([':cid' => $courseId]);

        $this->success($stmt->fetchAll());
    }

    // DELETE /api/courses/{id}/members/{studentId} (RF-13)
    public function removeMember(int $courseId, int $studentId): void
    {
        $payload = $this->requireTeacher();

        $course = $this->courseRepo->findByIdAsModel($courseId);
        if (!$course) {
            $this->error('Curso no encontrado', 404);
        }
        if ($course->getTeacherId() !== $payload['userId']) {
            $this->error('No tienes permisos sobre este curso', 403);
        }

        $pdo  = \DatabaseConnection::getInstance()->getPdo();
        $stmt = $pdo->prepare(
            "DELETE FROM enrollment WHERE course_id = :cid AND student_id = :sid"
        );
        $stmt->execute([':cid' => $courseId, ':sid' => $studentId]);

        if ($stmt->rowCount() === 0) {
            $this->error('Estudiante no encontrado en el curso', 404);
        }

        $this->success(['message' => 'Estudiante removido del curso']);
    }

    // Helper: genera un access_code único alfanumérico
    private function generateAccessCode(): string
    {
        do {
            $code = strtoupper(substr(bin2hex(random_bytes(4)), 0, 8));
        } while ($this->courseRepo->findByAccessCode($code));

        return $code;
    }
}
