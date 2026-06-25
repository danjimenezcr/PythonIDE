<?php
require_once __DIR__ . '/BaseController.php';
require_once __DIR__ . '/../Models/Models.php';
require_once __DIR__ . '/../Repositories/GroupRepository.php';
require_once __DIR__ . '/../Repositories/CourseAndSubmissionRepository.php';

/**
 * GroupController — Gestiona grupos de trabajo (RF-11, RF-12).
 * Endpoints:
 *   POST /api/groups                  → crear grupo (student)
 *   POST /api/groups/join             → unirse a grupo con código (student)
 *   GET  /api/courses/{id}/groups     → ver grupos del curso (any auth)
 *   PUT  /api/groups/{id}             → renombrar grupo (teacher)
 */
class GroupController extends BaseController
{
    private GroupRepository  $groupRepo;
    private CourseRepository $courseRepo;

    public function __construct()
    {
        $this->groupRepo  = new GroupRepository();
        $this->courseRepo = new CourseRepository();
    }

    // POST /api/groups (RF-11)
    // Body: { "course_id", "name" }
    public function createGroup(): void
    {
        $payload = $this->requireStudent();
        $body    = $this->getBody();

        if (empty($body['course_id']) || empty($body['name'])) {
            $this->error('course_id y name son obligatorios');
        }

        $pdo = \DatabaseConnection::getInstance()->getPdo();

        $stmt = $pdo->prepare(
            "SELECT id FROM enrollment WHERE student_id = :sid AND course_id = :cid"
        );
        $stmt->execute([':sid' => $payload['userId'], ':cid' => $body['course_id']]);
        if (!$stmt->fetch()) {
            $this->error('No estás inscrito en este curso', 403);
        }

        do {
            $inviteCode = strtoupper(substr(bin2hex(random_bytes(4)), 0, 8));
            $check = $pdo->prepare("SELECT id FROM student_group WHERE invite_code = :code");
            $check->execute([':code' => $inviteCode]);
        } while ($check->fetch());

        $stmt = $pdo->prepare(
            "INSERT INTO student_group (course_id, name, invite_code) VALUES (:cid, :name, :code)"
        );
        $stmt->execute([
            ':cid'  => $body['course_id'],
            ':name' => $body['name'],
            ':code' => $inviteCode,
        ]);
        $groupId = (int) $pdo->lastInsertId();

        $stmt = $pdo->prepare(
            "INSERT INTO group_membership (group_id, student_id) VALUES (:gid, :sid)"
        );
        $stmt->execute([':gid' => $groupId, ':sid' => $payload['userId']]);

        $this->success([
            'id'          => $groupId,
            'course_id'   => $body['course_id'],
            'name'        => $body['name'],
            'invite_code' => $inviteCode,
        ], 201);
    }

    // POST /api/groups/join (RF-11)
    // Body: { "invite_code" }
    public function joinGroup(): void
    {
        $payload = $this->requireStudent();
        $body    = $this->getBody();

        if (empty($body['invite_code'])) {
            $this->error('El código de invitación es obligatorio');
        }

        $pdo = \DatabaseConnection::getInstance()->getPdo();

        $stmt = $pdo->prepare(
            "SELECT * FROM student_group WHERE invite_code = :code LIMIT 1"
        );
        $stmt->execute([':code' => $body['invite_code']]);
        $group = $stmt->fetch();

        if (!$group) {
            $this->error('Código de invitación inválido');
        }

        $stmt = $pdo->prepare(
            "SELECT id FROM group_membership WHERE group_id = :gid AND student_id = :sid"
        );
        $stmt->execute([':gid' => $group['id'], ':sid' => $payload['userId']]);
        if ($stmt->fetch()) {
            $this->error('Ya eres miembro de este grupo');
        }

        $stmt = $pdo->prepare(
            "INSERT INTO group_membership (group_id, student_id) VALUES (:gid, :sid)"
        );
        $stmt->execute([':gid' => $group['id'], ':sid' => $payload['userId']]);

        $this->success([
            'message'     => 'Te uniste al grupo correctamente',
            'group_id'    => $group['id'],
            'group_name'  => $group['name'],
            'invite_code' => $group['invite_code'],
        ]);
    }

    // GET /api/courses/{id}/groups (RF-11)
    public function getGroupsByCourse(int $courseId): void
    {
        $payload = $this->requireAuth();
        $pdo     = \DatabaseConnection::getInstance()->getPdo();

        $stmt = $pdo->prepare("
            SELECT sg.id, sg.name, sg.invite_code, sg.created_at,
                   COUNT(gm.student_id) as member_count
              FROM student_group sg
              LEFT JOIN group_membership gm ON gm.group_id = sg.id
             WHERE sg.course_id = :cid
             GROUP BY sg.id
             ORDER BY sg.name
        ");
        $stmt->execute([':cid' => $courseId]);

        $this->success($stmt->fetchAll());
    }

    // GET /api/courses/{id}/groups para profesor (RF-11)
    public function getGroupsForCourse(int $courseId): void
    {
        $payload = $this->requireTeacher();

        $course = $this->courseRepo->findByIdAsModel($courseId);
        if (!$course) {
            $this->error('Curso no encontrado', 404);
        }
        if ($course->getTeacherId() !== $payload['userId']) {
            $this->error('No tienes permisos sobre este curso', 403);
        }

        $this->success($this->groupRepo->findByCourseWithMembers($courseId));
    }

    // PUT /api/groups/{id} (RF-11)
    // Body: { "name" }
    public function renameGroup(int $groupId): void
    {
        $payload = $this->requireTeacher();
        $body    = $this->getBody();

        if (empty($body['name'])) {
            $this->error('El nombre del grupo es obligatorio');
        }

        $group = $this->groupRepo->findByIdAsModel($groupId);
        if (!$group) {
            $this->error('Grupo no encontrado', 404);
        }

        $course = $this->courseRepo->findByIdAsModel($group->getCourseId());
        if ($course->getTeacherId() !== $payload['userId']) {
            $this->error('No tienes permisos sobre este grupo', 403);
        }

        $group->setName($body['name']);
        $updated = $this->groupRepo->save($group);
        $this->success($updated->toArray());
    }
}