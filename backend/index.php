<?php
/**
 * index.php: Punto de entrada de la API REST de PyStudio.
 * Todas las requests pasan por aquí. El router lee el método HTTP y la URL para despachar al Controller correcto.
 * Estructura de URLs:
 *   POST   /api/auth/register
 *   POST   /api/auth/login
 *   POST   /api/auth/logout
 *   POST   /api/courses
 *   GET    /api/courses
 *   POST   /api/courses/enroll
 *   PUT    /api/courses/{id}
 *   DELETE /api/courses/{id}
 *   GET    /api/courses/{id}/members
 *   DELETE /api/courses/{id}/members/{studentId}
 *   POST   /api/activities
 *   GET    /api/courses/{id}/activities
 *   GET    /api/activities/{id}
 *   PUT    /api/activities/{id}
 *   DELETE /api/activities/{id}
 *   POST   /api/submissions
 *   GET    /api/activities/{id}/submissions
 *   GET    /api/submissions/{id}
 */

header('Content-Type: application/json');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Authorization');

// Responder preflight CORS
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(204);
    exit;
}

require_once __DIR__ . '/Controllers/AuthController.php';
require_once __DIR__ . '/Controllers/CourseController.php';
require_once __DIR__ . '/Controllers/ActivityController.php';
require_once __DIR__ . '/Controllers/SubmissionAndSignatureController.php';

$method = $_SERVER['REQUEST_METHOD'];
$uri    = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);

// Limpiar prefijo /api o /backend/api si aplica
$uri = preg_replace('#^/[^/]+/api#', '/api', $uri);
$uri = rtrim($uri, '/');

// AUTH
if ($uri === '/api/auth/register' && $method === 'POST') {
    (new AuthController())->register();

} elseif ($uri === '/api/auth/login' && $method === 'POST') {
    (new AuthController())->login();

} elseif ($uri === '/api/auth/logout' && $method === 'POST') {
    (new AuthController())->logout();

// COURSES
} elseif ($uri === '/api/courses' && $method === 'POST') {
    (new CourseController())->createCourse();

} elseif ($uri === '/api/courses' && $method === 'GET') {
    (new CourseController())->getCourses();

} elseif ($uri === '/api/courses/enroll' && $method === 'POST') {
    (new CourseController())->enrollStudent();

} elseif (preg_match('#^/api/courses/(\d+)$#', $uri, $m) && $method === 'PUT') {
    (new CourseController())->updateCourse((int) $m[1]);

} elseif (preg_match('#^/api/courses/(\d+)$#', $uri, $m) && $method === 'DELETE') {
    (new CourseController())->deleteCourse((int) $m[1]);

} elseif (preg_match('#^/api/courses/(\d+)/members$#', $uri, $m) && $method === 'GET') {
    (new CourseController())->getMembers((int) $m[1]);

} elseif (preg_match('#^/api/courses/(\d+)/members/(\d+)$#', $uri, $m) && $method === 'DELETE') {
    (new CourseController())->removeMember((int) $m[1], (int) $m[2]);

// ACTIVITIES
} elseif ($uri === '/api/activities' && $method === 'POST') {
    (new ActivityController())->createActivity();

} elseif (preg_match('#^/api/courses/(\d+)/activities$#', $uri, $m) && $method === 'GET') {
    (new ActivityController())->getActivities((int) $m[1]);

} elseif (preg_match('#^/api/activities/(\d+)$#', $uri, $m) && $method === 'GET') {
    (new ActivityController())->getDetail((int) $m[1]);

} elseif (preg_match('#^/api/activities/(\d+)$#', $uri, $m) && $method === 'PUT') {
    (new ActivityController())->updateActivity((int) $m[1]);

} elseif (preg_match('#^/api/activities/(\d+)$#', $uri, $m) && $method === 'DELETE') {
    (new ActivityController())->deleteActivity((int) $m[1]);

// SUBMISSIONS
} elseif ($uri === '/api/submissions' && $method === 'POST') {
    (new SubmissionController())->submitActivity();

} elseif (preg_match('#^/api/activities/(\d+)/submissions$#', $uri, $m) && $method === 'GET') {
    (new SubmissionController())->getSubmissions((int) $m[1]);

} elseif (preg_match('#^/api/submissions/(\d+)$#', $uri, $m) && $method === 'GET') {
    (new SubmissionController())->getDetail((int) $m[1]);

// 404
} else {
    http_response_code(404);
    echo json_encode(['success' => false, 'message' => 'Endpoint no encontrado']);
}
