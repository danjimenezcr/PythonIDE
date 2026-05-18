<?php
require_once __DIR__ . '/BaseModel.php';

/**
 * Course: Curso creado por un profesor
 * El accessCode es único y generado automáticamente por CourseController al crear el curso (RF-03).
 */
class Course extends BaseModel
{
    private int     $teacherId;
    private string  $name;
    private ?string $description;
    private string  $accessCode;

    public function __construct(
        ?int    $id          = null,
        int     $teacherId   = 0,
        string  $name        = '',
        ?string $description = null,
        string  $accessCode  = '',
        ?string $createdAt   = null
    ) {
        parent::__construct($id, $createdAt);
        $this->teacherId   = $teacherId;
        $this->name        = $name;
        $this->description = $description;
        $this->accessCode  = $accessCode;
    }

    // Getters 
    public function getTeacherId(): int      { return $this->teacherId; }
    public function getName(): string        { return $this->name; }
    public function getDescription(): ?string{ return $this->description; }
    public function getAccessCode(): string  { return $this->accessCode; }

    // Setters 
    public function setName(string $name): void            { $this->name        = $name; }
    public function setDescription(?string $desc): void    { $this->description = $desc; }

    public function toArray(): array
    {
        return [
            'id'          => $this->id,
            'teacher_id'  => $this->teacherId,
            'name'        => $this->name,
            'description' => $this->description,
            'access_code' => $this->accessCode,
            'created_at'  => $this->createdAt,
        ];
    }
}
