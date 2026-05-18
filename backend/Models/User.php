<?php
require_once __DIR__ . '/BaseModel.php';

/**
 * User: Representa a estudiantes y profesores.
 * El campo role ('student' | 'teacher') controla los permisos en toda la aplicación.  
 * La contraseña NUNCA se almacena en texto plano; se usa password_hash() / password_verify().
 */
class User extends BaseModel
{
    private string $email;
    private string $passwordHash;
    private string $fullName;
    private string $role;           
    private bool   $isActive;

    public function __construct(
        ?int    $id           = null,
        string  $email        = '',
        string  $passwordHash = '',
        string  $fullName     = '',
        string  $role         = 'student',
        bool    $isActive     = true,
        ?string $createdAt    = null
    ) {
        parent::__construct($id, $createdAt);
        $this->email        = $email;
        $this->passwordHash = $passwordHash;
        $this->fullName     = $fullName;
        $this->role         = $role;
        $this->isActive     = $isActive;
    }

    // Getters 
    public function getEmail(): string        { return $this->email; }
    public function getPasswordHash(): string { return $this->passwordHash; }
    public function getFullName(): string     { return $this->fullName; }
    public function getRole(): string         { return $this->role; }
    public function isActive(): bool          { return $this->isActive; }

    // Setters 
    public function setFullName(string $name): void    { $this->fullName  = $name; }
    public function setEmail(string $email): void      { $this->email     = $email; }
    public function setIsActive(bool $active): void    { $this->isActive  = $active; }

    /**
     * Genera y almacena el hash de la contraseña.
     * Nunca guardar la contraseña original.
     */
    public function setPassword(string $plainPassword): void
    {
        $this->passwordHash = password_hash($plainPassword, PASSWORD_BCRYPT);
    }

    // Verifica si la contraseña ingresada coincide con el hash almacenado.
    public function verifyPassword(string $plainPassword): bool
    {
        return password_verify($plainPassword, $this->passwordHash);
    }

    public function isTeacher(): bool  { return $this->role === 'teacher'; }
    public function isStudent(): bool  { return $this->role === 'student'; }

    public function toArray(): array
    {
        return [
            'id'         => $this->id,
            'email'      => $this->email,
            'full_name'  => $this->fullName,
            'role'       => $this->role,
            'is_active'  => $this->isActive,
            'created_at' => $this->createdAt,
        ];
        // Nota: password_hash no se incluye en toArray() por seguridad.
    }
}
