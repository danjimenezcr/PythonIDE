<?php
require_once __DIR__ . '/BaseController.php';
require_once __DIR__ . '/../Models/User.php';
require_once __DIR__ . '/../Repositories/UserRepository.php';

/**
 * AuthController: Gestiona registro, login y logout (RF-01, RF-02, RF-14)
 * Endpoints:
 *   POST /api/auth/register
 *   POST /api/auth/login
 *   POST /api/auth/logout
 */
class AuthController extends BaseController
{
    private UserRepository $userRepo;

    public function __construct()
    {
        $this->userRepo = new UserRepository();
    }

    // POST /api/auth/register (RF-01)
    // Body: { "full_name", "email", "password", "role" }
    public function register(): void
    {
        $body = $this->getBody();

        // Validar campos obligatorios
        if (empty($body['full_name']) || empty($body['email']) ||
            empty($body['password']) || empty($body['role'])) {
            $this->error('Todos los campos son obligatorios');
        }

        // Validar rol
        if (!in_array($body['role'], ['student', 'teacher'])) {
            $this->error('Rol inválido. Debe ser student o teacher');
        }

        // Validar formato de email
        if (!filter_var($body['email'], FILTER_VALIDATE_EMAIL)) {
            $this->error('Formato de correo electrónico inválido');
        }

        // Validar que el email no este registrado
        if ($this->userRepo->findByEmail($body['email'])) {
            $this->error('El correo electrónico ya está registrado');
        }

        // Validar longitud minima de contraseña
        if (strlen($body['password']) < 8) {
            $this->error('La contraseña debe tener al menos 8 caracteres');
        }

        // Crear y guardar el usuario
        $user = new User(
            null,
            $body['email'],
            '',
            $body['full_name'],
            $body['role']
        );
        $user->setPassword($body['password']);

        $saved = $this->userRepo->save($user);

        $this->success($saved->toArray(), 201);
    }

    // POST /api/auth/login (RF-02)
    // Body: { "email", "password" }
    public function login(): void
    {
        $body = $this->getBody();

        if (empty($body['email']) || empty($body['password'])) {
            $this->error('Correo y contraseña son obligatorios');
        }

        $user = $this->userRepo->findByEmail($body['email']);

        if (!$user || !$user->verifyPassword($body['password'])) {
            $this->error('Correo o contraseña incorrectos', 401);
        }

        if (!$user->isActive()) {
            $this->error('La cuenta ha sido desactivada', 401);
        }

        // Generar JWT con id, email y role
        $token = $this->generateJWT([
            'userId' => $user->getId(),
            'email'  => $user->getEmail(),
            'role'   => $user->getRole(),
        ]);

        $this->success([
            'token' => $token,
            'user'  => $user->toArray(),
        ]);
    }

    // POST /api/auth/logout (RF-14)
    // Header: Authorization: Bearer <token>
    // El cliente simplemente descarta el token.
    public function logout(): void
    {
        $this->requireAuth();
        // Con JWT stateless, el logout se maneja en el cliente descartando el token. El servidor confirma la acción.
        $this->success(['message' => 'Sesión cerrada correctamente']);
    }
}
