<?php
require_once __DIR__ . '/BaseRepository.php';
require_once __DIR__ . '/../Models/Models.php';

// GroupRepository — Persistencia del modelo StudentGroup (RF-11)
class GroupRepository extends BaseRepository
{
    public function __construct()
    {
        parent::__construct();
        $this->table = 'student_group';
    }

    public function save(BaseModel $model): BaseModel
    {
        /** @var StudentGroup $model */
        if ($model->getId() === null) {
            $stmt = $this->pdo->prepare("
                INSERT INTO student_group (course_id, name, invite_code)
                VALUES (:course_id, :name, :invite_code)
            ");
            $stmt->execute([
                ':course_id'   => $model->getCourseId(),
                ':name'        => $model->getName(),
                ':invite_code' => $model->getInviteCode(),
            ]);
            return $this->findByIdAsModel((int) $this->pdo->lastInsertId());
        }

        $stmt = $this->pdo->prepare("UPDATE student_group SET name = :name WHERE id = :id");
        $stmt->execute([
            ':name' => $model->getName(),
            ':id'   => $model->getId(),
        ]);
        return $model;
    }

    // Todos los grupos de un curso, con sus miembros (RF-11)
    public function findByCourseWithMembers(int $courseId): array
    {
        $stmt = $this->pdo->prepare(
            "SELECT * FROM student_group WHERE course_id = :cid ORDER BY name"
        );
        $stmt->execute([':cid' => $courseId]);
        $groups = $stmt->fetchAll();

        $memberStmt = $this->pdo->prepare("
            SELECT u.id, u.full_name, u.email
              FROM group_membership gm
              JOIN user u ON u.id = gm.student_id
             WHERE gm.group_id = :gid
             ORDER BY u.full_name
        ");

        foreach ($groups as &$group) {
            $memberStmt->execute([':gid' => $group['id']]);
            $group['members'] = $memberStmt->fetchAll();
        }

        return $groups;
    }

    public function findByIdAsModel(int $id): ?StudentGroup
    {
        $row = $this->findById($id);
        return $row ? $this->hydrate($row) : null;
    }

    private function hydrate(array $row): StudentGroup
    {
        return new StudentGroup(
            (int) $row['id'],
            (int) $row['course_id'],
                  $row['name'],
                  $row['invite_code'],
                  $row['created_at']
        );
    }
}
