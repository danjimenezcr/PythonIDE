<?php
require_once __DIR__ . '/BaseRepository.php';
require_once __DIR__ . '/../Models/User.php';

/**
 * UserRepository: Persistencia del modelo User.
 * Hereda los métodos genéricos de BaseRepository y añade consultas específicas del dominio de usuarios.
 */
class UserRepository extends BaseRepository
{
    public function __construct()
    {
        parent::__construct();
        $this->table = 'user';
    }

    // INSERT si id es null, UPDATE si ya tiene id.
    public function save(BaseModel $model): BaseModel
    {
        /** @var User $model */
        if ($model->getId() === null) {
            $stmt = $this->pdo->prepare("
                INSERT INTO user (email, password_hash, full_name, role, is_active)
                VALUES (:email, :password_hash, :full_name, :role, :is_active)
            ");
            $stmt->execute([
                ':email'         => $model->getEmail(),
                ':password_hash' => $model->getPasswordHash(),
                ':full_name'     => $model->getFullName(),
                ':role'          => $model->getRole(),
                ':is_active'     => $model->isActive() ? 1 : 0,
            ]);
            // Devuelve el modelo con el id recién generado
            return $this->findByIdAsModel((int) $this->pdo->lastInsertId());
        }

        $stmt = $this->pdo->prepare("
            UPDATE user
               SET email         = :email,
                   full_name     = :full_name,
                   is_active     = :is_active
             WHERE id = :id
        ");
        $stmt->execute([
            ':email'     => $model->getEmail(),
            ':full_name' => $model->getFullName(),
            ':is_active' => $model->isActive() ? 1 : 0,
            ':id'        => $model->getId(),
        ]);
        return $model;
    }

    // Busca por email; retorna User o null 
    public function findByEmail(string $email): ?User
    {
        $stmt = $this->pdo->prepare(
            "SELECT * FROM user WHERE email = :email LIMIT 1"
        );
        $stmt->execute([':email' => $email]);
        $row = $stmt->fetch();
        return $row ? $this->hydrate($row) : null;
    }

    // Busca por id y retorna un objeto User o null 
    public function findByIdAsModel(int $id): ?User
    {
        $row = $this->findById($id);
        return $row ? $this->hydrate($row) : null;
    }

    // Convierte una fila de BD en un objeto User 
    private function hydrate(array $row): User
    {
        return new User(
            (int)    $row['id'],
                     $row['email'],
                     $row['password_hash'],
                     $row['full_name'],
                     $row['role'],
            (bool)   $row['is_active'],
                     $row['created_at']
        );
    }
}
