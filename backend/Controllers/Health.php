<?php
require_once __DIR__ . '/../Database/DatabaseConnection.php';

/**
 * Health — Verifica el estado del servidor y la base de datos.
 * GET /api/health
 */
class Health
{
    private array $requiredTables = [
        'user',
        'course',
        'enrollment',
        'activity',
        'activity_file',
        'student_group',
        'group_membership',
        'submission',
        'submission_file',
        'git_commit',
    ];

    public function check(): void
    {
        header('Content-Type: application/json');

        $result = [
            'healthy'     => false,
            'php_version' => PHP_VERSION,
            'database'    => 'not checked',
            'tables'      => [],
        ];

        try {
            $pdo = DatabaseConnection::getInstance()->getPdo();
            $result['database'] = 'ok';

            $dbname = getenv('DB_NAME') ?: 'pystudio';

            $stmt = $pdo->prepare("
                SELECT TABLE_NAME
                  FROM information_schema.TABLES
                 WHERE TABLE_SCHEMA = :dbname
            ");
            $stmt->execute([':dbname' => $dbname]);

            $existingTables   = array_column($stmt->fetchAll(PDO::FETCH_ASSOC), 'TABLE_NAME');
            $allTablesPresent = true;

            foreach ($this->requiredTables as $table) {
                $exists = in_array($table, $existingTables);
                $result['tables'][$table] = $exists;
                if (!$exists) {
                    $allTablesPresent = false;
                }
            }

            $result['healthy'] = $allTablesPresent;

        } catch (PDOException $e) {
            $result['database'] = 'error: ' . $e->getMessage();
            $result['healthy']  = false;
        }

        http_response_code($result['healthy'] ? 200 : 503);
        echo json_encode($result, JSON_PRETTY_PRINT);
        exit;
    }
}
