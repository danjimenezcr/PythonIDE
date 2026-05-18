<?php
require_once __DIR__ . '/BaseRepository.php';
require_once __DIR__ . '/../Models/Course.php';
require_once __DIR__ . '/../Models/Models.php';

// CourseRepository — Persistencia del modelo Course (RF-03/17).
class CourseRepository extends BaseRepository
{
    public function __construct()
    {
        parent::__construct();
        $this->table = 'course';
    }

    public function save(BaseModel $model): BaseModel
    {
        /** @var Course $model */
        if ($model->getId() === null) {
            $stmt = $this->pdo->prepare("
                INSERT INTO course (teacher_id, name, description, access_code)
                VALUES (:teacher_id, :name, :description, :access_code)
            ");
            $stmt->execute([
                ':teacher_id'  => $model->getTeacherId(),
                ':name'        => $model->getName(),
                ':description' => $model->getDescription(),
                ':access_code' => $model->getAccessCode(),
            ]);
            return $this->findByIdAsModel((int) $this->pdo->lastInsertId());
        }

        $stmt = $this->pdo->prepare("
            UPDATE course
               SET name        = :name,
                   description = :description
             WHERE id = :id
        ");
        $stmt->execute([
            ':name'        => $model->getName(),
            ':description' => $model->getDescription(),
            ':id'          => $model->getId(),
        ]);
        return $model;
    }

    // Todos los cursos de un profesor 
    public function findByTeacher(int $teacherId): array
    {
        $stmt = $this->pdo->prepare(
            "SELECT * FROM course WHERE teacher_id = :tid ORDER BY created_at DESC"
        );
        $stmt->execute([':tid' => $teacherId]);
        return array_map([$this, 'hydrate'], $stmt->fetchAll());
    }

    // Todos los cursos en los que está inscrito un estudiante 
    public function findByStudent(int $studentId): array
    {
        $stmt = $this->pdo->prepare("
            SELECT c.*
              FROM course c
              JOIN enrollment e ON e.course_id = c.id
             WHERE e.student_id = :sid
             ORDER BY c.name
        ");
        $stmt->execute([':sid' => $studentId]);
        return array_map([$this, 'hydrate'], $stmt->fetchAll());
    }

    // Busca un curso por su access_code 
    public function findByAccessCode(string $code): ?Course
    {
        $stmt = $this->pdo->prepare(
            "SELECT * FROM course WHERE access_code = :code LIMIT 1"
        );
        $stmt->execute([':code' => $code]);
        $row = $stmt->fetch();
        return $row ? $this->hydrate($row) : null;
    }

    public function findByIdAsModel(int $id): ?Course
    {
        $row = $this->findById($id);
        return $row ? $this->hydrate($row) : null;
    }

    private function hydrate(array $row): Course
    {
        return new Course(
            (int) $row['id'],
            (int) $row['teacher_id'],
                  $row['name'],
                  $row['description'],
                  $row['access_code'],
                  $row['created_at']
        );
    }
}

// SubmissionRepository

// SubmissionRepository — Persistencia del modelo Submission (RF-09/10/12)
class SubmissionRepository extends BaseRepository
{
    public function __construct()
    {
        parent::__construct();
        $this->table = 'submission';
    }

    public function save(BaseModel $model): BaseModel
    {
        /** @var Submission $model */
        if ($model->getId() === null) {
            $stmt = $this->pdo->prepare("
                INSERT INTO submission
                    (activity_id, student_id, group_id, is_group_submission, submitted_at, signature_valid)
                VALUES
                    (:activity_id, :student_id, :group_id, :is_group_submission, NOW(), :signature_valid)
            ");
            $stmt->execute([
                ':activity_id'         => $model->getActivityId(),
                ':student_id'          => $model->getStudentId(),
                ':group_id'            => $model->getGroupId(),
                ':is_group_submission' => $model->isGroupSubmission() ? 1 : 0,
                ':signature_valid'     => $model->isSignatureValid() ? 1 : 0,
            ]);
            return $this->findByIdAsModel((int) $this->pdo->lastInsertId());
        }

        // Solo se actualiza signature_valid (RF-19)
        $stmt = $this->pdo->prepare("
            UPDATE submission SET signature_valid = :sv WHERE id = :id
        ");
        $stmt->execute([
            ':sv' => $model->isSignatureValid() ? 1 : 0,
            ':id' => $model->getId(),
        ]);
        return $model;
    }

    // Todas las entregas de una actividad 
    public function findByActivity(int $activityId): array
    {
        $stmt = $this->pdo->prepare(
            "SELECT * FROM submission WHERE activity_id = :aid ORDER BY submitted_at DESC"
        );
        $stmt->execute([':aid' => $activityId]);
        return array_map([$this, 'hydrate'], $stmt->fetchAll());
    }

    public function findByIdAsModel(int $id): ?Submission
    {
        $row = $this->findById($id);
        return $row ? $this->hydrate($row) : null;
    }

    private function hydrate(array $row): Submission
    {
        return new Submission(
            (int)  $row['id'],
            (int)  $row['activity_id'],
                   $row['student_id'] !== null ? (int) $row['student_id'] : null,
                   $row['group_id']   !== null ? (int) $row['group_id']   : null,
            (bool) $row['is_group_submission'],
                   $row['submitted_at'],
            (bool) $row['signature_valid']
        );
    }
}