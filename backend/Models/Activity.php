<?php
require_once __DIR__ . '/BaseModel.php';

/**
 * Activity: Actividad académica dentro de un curso (RF-05).
 * Puede tener archivos adjuntos opcionales representados por la tabla activity_file.
 */
class Activity extends BaseModel
{
    private int     $courseId;
    private string  $title;
    private ?string $description;
    private string  $deadline;    

    public function __construct(
        ?int    $id          = null,
        int     $courseId    = 0,
        string  $title       = '',
        ?string $description = null,
        string  $deadline    = '',
        ?string $createdAt   = null
    ) {
        parent::__construct($id, $createdAt);
        $this->courseId    = $courseId;
        $this->title       = $title;
        $this->description = $description;
        $this->deadline    = $deadline;
    }

    // Getters 
    public function getCourseId(): int       { return $this->courseId; }
    public function getTitle(): string       { return $this->title; }
    public function getDescription(): ?string{ return $this->description; }
    public function getDeadline(): string    { return $this->deadline; }

    // Setters 
    public function setTitle(string $title): void          { $this->title       = $title; }
    public function setDescription(?string $desc): void    { $this->description = $desc; }
    public function setDeadline(string $deadline): void    { $this->deadline    = $deadline; }

    // Verifica si la actividad ya venció 
    public function isPastDeadline(): bool
    {
        return strtotime($this->deadline) < time();
    }

    public function toArray(): array
    {
        return [
            'id'          => $this->id,
            'course_id'   => $this->courseId,
            'title'       => $this->title,
            'description' => $this->description,
            'deadline'    => $this->deadline,
            'created_at'  => $this->createdAt,
        ];
    }
}
