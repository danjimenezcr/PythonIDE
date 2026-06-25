<?php
require_once __DIR__ . '/BaseController.php';
require_once __DIR__ . '/../Models/Models.php';
require_once __DIR__ . '/../Repositories/GroupRepository.php';
require_once __DIR__ . '/../Repositories/CourseAndSubmissionRepository.php';

/**
 * GroupController — Vista del profesor sobre los grupos de trabajo (RF-11).
 * La creación/unión a grupos es una acción del estudiante y se gestiona
 * desde el cliente de escritorio, no desde la web.
 *
 * Endpoints:
 *   GET /api/courses/{id}/groups - getGroupsForCourse (teacher)
 *   PUT /api/groups/{id}         - renameGroup (teacher)
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

    // GET /api/courses/{id}/groups (RF-11)
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
