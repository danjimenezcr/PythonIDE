<?php
require_once __DIR__ . '/../Database/DatabaseConnection.php';

/**
 * BaseRepository: Clase abstracta para todos los repositorios.
 * Inyecta el PDO compartido (Singleton) y expone los métodos CRUD genéricos que los repositorios concretos pueden sobrescribir o extender.
 */
abstract class BaseRepository
{
    protected PDO    $pdo;
    protected string $table;   

    public function __construct()
    {
        $this->pdo = DatabaseConnection::getInstance()->getPdo();
    }

    // Busca un registro por su PK y retorna el array asociativo, o null si no existe.
    public function findById(int $id): ?array
    {
        $stmt = $this->pdo->prepare(
            "SELECT * FROM {$this->table} WHERE id = :id LIMIT 1"
        );
        $stmt->execute([':id' => $id]);
        $row = $stmt->fetch();
        return $row ?: null;
    }

    /**
     * Devuelve todos los registros de la tabla.
     * Para tablas grandes, preferir métodos con filtros en los repositorios concretos.
     */
    public function findAll(): array
    {
        $stmt = $this->pdo->query("SELECT * FROM {$this->table}");
        return $stmt->fetchAll();
    }

    /**
     * Elimina un registro por su PK.
     * Retorna true si se eliminó al menos una fila.
     */
    public function deleteById(int $id): bool
    {
        $stmt = $this->pdo->prepare(
            "DELETE FROM {$this->table} WHERE id = :id"
        );
        $stmt->execute([':id' => $id]);
        return $stmt->rowCount() > 0;
    }

    // Cada repositorio concreto debe saber cómo persistir su modelo específico.
    abstract public function save(BaseModel $model): BaseModel;
}
