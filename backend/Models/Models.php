<?php
require_once __DIR__ . '/BaseModel.php';

// Enrollment: Matricula de un estudiante en un curso (RF-04).
class Enrollment extends BaseModel
{
    private int     $studentId;
    private int     $courseId;
    private ?string $enrolledAt;

    public function __construct(
        ?int    $id         = null,
        int     $studentId  = 0,
        int     $courseId   = 0,
        ?string $enrolledAt = null
    ) {
        parent::__construct($id, $enrolledAt);
        $this->studentId  = $studentId;
        $this->courseId   = $courseId;
        $this->enrolledAt = $enrolledAt;
    }

    public function getStudentId(): int  { return $this->studentId; }
    public function getCourseId(): int   { return $this->courseId; }
    public function getEnrolledAt(): ?string { return $this->enrolledAt; }

    public function toArray(): array
    {
        return [
            'id'          => $this->id,
            'student_id'  => $this->studentId,
            'course_id'   => $this->courseId,
            'enrolled_at' => $this->enrolledAt,
        ];
    }
}


// StudentGroup: Grupo de trabajo dentro de un curso (RF-11).
class StudentGroup extends BaseModel
{
    private int    $courseId;
    private string $name;
    private string $inviteCode;

    public function __construct(
        ?int    $id         = null,
        int     $courseId   = 0,
        string  $name       = '',
        string  $inviteCode = '',
        ?string $createdAt  = null
    ) {
        parent::__construct($id, $createdAt);
        $this->courseId   = $courseId;
        $this->name       = $name;
        $this->inviteCode = $inviteCode;
    }

    public function getCourseId(): int    { return $this->courseId; }
    public function getName(): string     { return $this->name; }
    public function getInviteCode(): string { return $this->inviteCode; }

    public function setName(string $name): void { $this->name = $name; }

    public function toArray(): array
    {
        return [
            'id'          => $this->id,
            'course_id'   => $this->courseId,
            'name'        => $this->name,
            'invite_code' => $this->inviteCode,
            'created_at'  => $this->createdAt,
        ];
    }
}


/**
 * Submission: Entrega de una actividad, individual o grupal (RF-09/12).
 * Exactamente uno de studentId / groupId debe tener valor, el otro será null. signature_valid refleja el estado de la firma digital (RF-18/19).
 */
class Submission extends BaseModel
{
    private int     $activityId;
    private ?int    $studentId;
    private ?int    $groupId;
    private bool    $isGroupSubmission;
    private string  $submittedAt;
    private bool    $signatureValid;

    public function __construct(
        ?int    $id                = null,
        int     $activityId        = 0,
        ?int    $studentId         = null,
        ?int    $groupId           = null,
        bool    $isGroupSubmission = false,
        string  $submittedAt       = '',
        bool    $signatureValid    = true
    ) {
        parent::__construct($id, $submittedAt);
        $this->activityId        = $activityId;
        $this->studentId         = $studentId;
        $this->groupId           = $groupId;
        $this->isGroupSubmission = $isGroupSubmission;
        $this->submittedAt       = $submittedAt;
        $this->signatureValid    = $signatureValid;
    }

    public function getActivityId(): int        { return $this->activityId; }
    public function getStudentId(): ?int        { return $this->studentId; }
    public function getGroupId(): ?int          { return $this->groupId; }
    public function isGroupSubmission(): bool   { return $this->isGroupSubmission; }
    public function getSubmittedAt(): string    { return $this->submittedAt; }
    public function isSignatureValid(): bool    { return $this->signatureValid; }

    public function setSignatureValid(bool $valid): void { $this->signatureValid = $valid; }

    public function toArray(): array
    {
        return [
            'id'                  => $this->id,
            'activity_id'         => $this->activityId,
            'student_id'          => $this->studentId,
            'group_id'            => $this->groupId,
            'is_group_submission' => $this->isGroupSubmission,
            'submitted_at'        => $this->submittedAt,
            'signature_valid'     => $this->signatureValid,
        ];
    }
}


/**
 * GitCommit: Commit automático generado por el IDE (RF-20).
 * Almacena el hash, mensaje y timestamp de cada guardado para que el profesor pueda ver la evolución del código.
 */
class GitCommit extends BaseModel
{
    private int    $submissionId;
    private string $commitHash;
    private string $message;
    private string $committedAt;

    public function __construct(
        ?int   $id           = null,
        int    $submissionId = 0,
        string $commitHash   = '',
        string $message      = '',
        string $committedAt  = ''
    ) {
        parent::__construct($id, $committedAt);
        $this->submissionId = $submissionId;
        $this->commitHash   = $commitHash;
        $this->message      = $message;
        $this->committedAt  = $committedAt;
    }

    public function getSubmissionId(): int  { return $this->submissionId; }
    public function getCommitHash(): string { return $this->commitHash; }
    public function getMessage(): string    { return $this->message; }
    public function getCommittedAt(): string{ return $this->committedAt; }

    public function toArray(): array
    {
        return [
            'id'            => $this->id,
            'submission_id' => $this->submissionId,
            'commit_hash'   => $this->commitHash,
            'message'       => $this->message,
            'committed_at'  => $this->committedAt,
        ];
    }
}
