<?php
/**
 * BaseModel: Clase abstracta base para todos los modelos.
 * Centraliza los atributos comunes (id, created_at) y obliga a cada modelo concreto a implementar toArray() para facilitar los JSON.
 */
abstract class BaseModel
{
    protected ?int $id;
    protected ?string $createdAt;

    public function __construct(?int $id = null, ?string $createdAt = null)
    {
        $this->id        = $id;
        $this->createdAt = $createdAt;
    }

    public function getId(): ?int
    {
        return $this->id;
    }

    public function getCreatedAt(): ?string
    {
        return $this->createdAt;
    }

    // Cada modelo devuelve su estado como array asociativo.
    abstract public function toArray(): array;

    //Adding comment to test commit

}
