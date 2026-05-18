<?php
require_once __DIR__ . '/BaseController.php';
require_once __DIR__ . '/../Models/Activity.php';
require_once __DIR__ . '/../Repositories/CourseAndSubmissionRepository.php';

/**
 * ActivityController: Gestiona actividades dentro de cursos (RF-05, RF-06).
 * Endpoints:
 *   POST   /api/activities - createActivity (teacher)
 *   GET    /api/courses/{id}/activities - getActivities (any auth)
 *   GET    /api/activities/{id} - getDetail (any auth)
 *   PUT    /api/activities/{id} - updateActivity (teacher)
 *   DELETE /api/activities/{id} - deleteActivity (teacher)
 */
class ActivityController extends BaseController
{
    private CourseRepository $courseRepo;

    public function __construct()
    {
        $this->courseRepo = new CourseRepository();
    }

    private function getPdo(): \PDO
    {
        return \DatabaseConnection::getInstance()->getPdo();
    }

    // POST /api/activities (RF-05)
    // Body: { "course_id", "title", "description", "deadline" }
    public function createActivity(): void
    {
        $payload = $this->requireTeacher();
        $body    = $this->getBody();

        if (empty($body['course_id']) || empty($body['title']) || empty($body['deadline'])) {
            $this->error('course_id, title y deadline son obligatorios');
        }

        // Verificar que el curso pertenece al profesor
        $course = $this->courseRepo->findByIdAsModel((int) $body['course_id']);
        if (!$course) {
            $this->error('Curso no encontrado', 404);
        }
        if ($course->getTeacherId() !== $payload['userId']) {
            $this->error('No tienes permisos sobre este curso', 403);
        }

        $pdo  = $this->getPdo();
        $stmt = $pdo->prepare("
            INSERT INTO activity (course_id, title, description, deadline)
            VALUES (:course_id, :title, :description, :deadline)
        ");
        $stmt->execute([
            ':course_id'   => $body['course_id'],
            ':title'       => $body['title'],
            ':description' => $body['description'] ?? null,
            ':deadline'    => $body['deadline'],
        ]);

        $activityId = (int) $pdo->lastInsertId();
        $activity   = $this->findActivityById($activityId);

        $this->success($activity, 201);
    }

    // GET /api/courses/{courseId}/activities (RF-06)
    public function getActivities(int $courseId): void
    {
        $this->requireAuth();

        $stmt = $this->getPdo()->prepare(
            "SELECT * FROM activity WHERE course_id = :cid ORDER BY deadline ASC"
        );
        $stmt->execute([':cid' => $courseId]);

        $this->success($stmt->fetchAll());
    }

    // GET /api/activities/{id} (RF-06)
    public function getDetail(int $activityId): void
    {
        $this->requireAuth();

        $activity = $this->findActivityById($activityId);
        if (!$activity) {
            $this->error('Actividad no encontrada', 404);
        }

        $this->success($activity);
    }

    // PUT /api/activities/{id} (RF-05)
    // Body: { "title", "description", "deadline" }
    public function updateActivity(int $activityId): void
    {
        $payload  = $this->requireTeacher();
        $body     = $this->getBody();
        $activity = $this->findActivityById($activityId);

        if (!$activity) {
            $this->error('Actividad no encontrada', 404);
        }

        // Verificar que el curso pertenece al profesor
        $course = $this->courseRepo->findByIdAsModel($activity['course_id']);
        if ($course->getTeacherId() !== $payload['userId']) {
            $this->error('No tienes permisos sobre esta actividad', 403);
        }

        $title       = $body['title']       ?? $activity['title'];
        $description = $body['description'] ?? $activity['description'];
        $deadline    = $body['deadline']    ?? $activity['deadline'];

        $stmt = $this->getPdo()->prepare("
            UPDATE activity
               SET title = :title, description = :description, deadline = :deadline
             WHERE id = :id
        ");
        $stmt->execute([
            ':title'       => $title,
            ':description' => $description,
            ':deadline'    => $deadline,
            ':id'          => $activityId,
        ]);

        $this->success($this->findActivityById($activityId));
    }

    // DELETE /api/activities/{id} (RF-05)
    public function deleteActivity(int $activityId): void
    {
        $payload  = $this->requireTeacher();
        $activity = $this->findActivityById($activityId);

        if (!$activity) {
            $this->error('Actividad no encontrada', 404);
        }

        $course = $this->courseRepo->findByIdAsModel($activity['course_id']);
        if ($course->getTeacherId() !== $payload['userId']) {
            $this->error('No tienes permisos sobre esta actividad', 403);
        }

        $stmt = $this->getPdo()->prepare("DELETE FROM activity WHERE id = :id");
        $stmt->execute([':id' => $activityId]);

        $this->success(['message' => 'Actividad eliminada correctamente']);
    }

    // Helper: busca una actividad por id como array
    private function findActivityById(int $id): ?array
    {
        $stmt = $this->getPdo()->prepare(
            "SELECT * FROM activity WHERE id = :id LIMIT 1"
        );
        $stmt->execute([':id' => $id]);
        $row = $stmt->fetch();
        return $row ?: null;
    }
}
