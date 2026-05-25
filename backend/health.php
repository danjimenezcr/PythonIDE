<?php
/**
 * health.php — Endpoint de verificación del estado del servidor.
 * Comprueba que PHP, la conexión a la base de datos y todas las tablas
 * requeridas estén disponibles. No requiere autenticación.
 *
 * GET /health.php
 *
 * Respuesta exitosa (HTTP 200):
 * {
 *   "healthy": true,
 *   "php_version": "8.2.x",
 *   "database": "ok",
 *   "tables": { "user": true, "course": true, ... }
 * }
 *
 * Respuesta con error (HTTP 503):
 * {
 *   "healthy": false,
 *   "php_version": "8.2.x",
 *   "database": "error: <message>",
 *   "tables": {}
 * }
 */

header('Content-Type: application/json');

// Tables that must exist for the app to function correctly
$requiredTables = [
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

$result = [
    'healthy'     => false,
    'php_version' => PHP_VERSION,
    'database'    => 'not checked',
    'tables'      => [],
];

try {
    // Attempt DB connection using the same config as the rest of the app
    $host     = $_ENV['DB_HOST']     ?? 'localhost';
    $dbname   = $_ENV['DB_NAME']     ?? 'pystudio';
    $user     = $_ENV['DB_USER']     ?? 'pystudio';
    $password = $_ENV['DB_PASSWORD'] ?? '';

    $pdo = new PDO(
        "mysql:host={$host};dbname={$dbname};charset=utf8mb4",
        $user,
        $password,
        [PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION]
    );

    $result['database'] = 'ok';

    // Check each required table exists in the database
    $stmt = $pdo->prepare("
        SELECT TABLE_NAME
          FROM information_schema.TABLES
         WHERE TABLE_SCHEMA = :dbname
    ");
    $stmt->execute([':dbname' => $dbname]);

    $existingTables = array_column($stmt->fetchAll(PDO::FETCH_ASSOC), 'TABLE_NAME');

    $allTablesPresent = true;
    foreach ($requiredTables as $table) {
        $exists = in_array($table, $existingTables);
        $result['tables'][$table] = $exists;
        if (!$exists) {
            $allTablesPresent = false;
        }
    }

    $result['healthy'] = $allTablesPresent;

} catch (PDOException $e) {
    // DB connection failed — surface the error message for diagnosis
    $result['database'] = 'error: ' . $e->getMessage();
    $result['healthy']  = false;
}

// Return 200 if healthy, 503 if not — makes it easy to use with monitoring tools
http_response_code($result['healthy'] ? 200 : 503);
echo json_encode($result, JSON_PRETTY_PRINT);
