<?php
/**
 * DatabaseConnection — Patrón Singleton
 * Una única instancia del pool PDO para toda la aplicación.
 * Cualquier repositorio llama DatabaseConnection::getInstance() en lugar de crear su propia conexión.
 */
class DatabaseConnection
{
    private static ?DatabaseConnection $instance = null;
    private PDO $pdo;

    private function __construct()
    {
        $host     = getenv('DB_HOST')     ?: 'localhost';
        $dbname   = getenv('DB_NAME')     ?: 'pystudio';
        $user     = getenv('DB_USER')     ?: 'root';
        $password = getenv('DB_PASSWORD') ?: '';
        $charset  = 'utf8mb4';

        $dsn = "mysql:host={$host};dbname={$dbname};charset={$charset}";

        $this->pdo = new PDO($dsn, $user, $password, [
            PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES   => false,
        ]);
    }

    // No se permite clonar el singleton 
    private function __clone() {}

    public static function getInstance(): DatabaseConnection
    {
        if (self::$instance === null) {
            self::$instance = new self();
        }
        return self::$instance;
    }

    public function getPdo(): PDO
    {
        return $this->pdo;
    }
}
