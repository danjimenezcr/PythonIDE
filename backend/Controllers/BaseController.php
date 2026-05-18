<?php
/**
 * BaseController: Clase abstracta base para todos los controllers.
 * Centraliza el manejo de respuestas JSON y la validación JWT.
 * Todos los controllers heredan de esta clase.
 */
abstract class BaseController
{
    // Clave secreta para firmar los JWT
    private static string $jwtSecret = 'PYSTUDIO_SECRET_KEY_2026';

    // Respuestas JSON

    // Respuesta exitosa 
    protected function success(mixed $data = null, int $code = 200): void
    {
        http_response_code($code);
        header('Content-Type: application/json');
        echo json_encode([
            'success' => true,
            'data'    => $data,
        ]);
        exit;
    }

    // Respuesta de error 
    protected function error(string $message, int $code = 400): void
    {
        http_response_code($code);
        header('Content-Type: application/json');
        echo json_encode([
            'success' => false,
            'message' => $message,
        ]);
        exit;
    }

    // Lee y decodifica el body JSON de la request 
    protected function getBody(): array
    {
        $raw = file_get_contents('php://input');
        return json_decode($raw, true) ?? [];
    }

    // JWT: Generación y validación

    /**
     * Genera un token JWT con el payload dado.
     * Expira en 8 horas por defecto.
     */
    protected function generateJWT(array $payload, int $expiresInSeconds = 28800): string
    {
        $header = base64_encode(json_encode(['alg' => 'HS256', 'typ' => 'JWT']));

        $payload['iat'] = time();
        $payload['exp'] = time() + $expiresInSeconds;
        $encodedPayload  = base64_encode(json_encode($payload));

        $signature = hash_hmac(
            'sha256',
            "{$header}.{$encodedPayload}",
            self::$jwtSecret,
            true
        );
        $encodedSignature = base64_encode($signature);

        return "{$header}.{$encodedPayload}.{$encodedSignature}";
    }

    /**
     * Valida el JWT del header Authorization.
     * Retorna el payload decodificado o llama a error() si es inválido.
     */
    protected function requireAuth(): array
    {
        $authHeader = $_SERVER['HTTP_AUTHORIZATION'] ?? '';

        if (!str_starts_with($authHeader, 'Bearer ')) {
            $this->error('Token no proporcionado', 401);
        }

        $token  = substr($authHeader, 7);
        $parts  = explode('.', $token);

        if (count($parts) !== 3) {
            $this->error('Token inválido', 401);
        }

        [$header, $payload, $signature] = $parts;

        // Verificar firma
        $expectedSig = base64_encode(
            hash_hmac('sha256', "{$header}.{$payload}", self::$jwtSecret, true)
        );

        if (!hash_equals($expectedSig, $signature)) {
            $this->error('Token inválido', 401);
        }

        // Verificar expiración
        $decoded = json_decode(base64_decode($payload), true);
        if ($decoded['exp'] < time()) {
            $this->error('Token expirado', 401);
        }

        return $decoded;
    }

    /**
     * Verifica que el usuario autenticado sea teacher.
     * Retorna el payload si es teacher, error 403 si no.
     */
    protected function requireTeacher(): array
    {
        $payload = $this->requireAuth();
        if ($payload['role'] !== 'teacher') {
            $this->error('Acceso restringido a profesores', 403);
        }
        return $payload;
    }

    // Verifica que el usuario autenticado sea student.
    protected function requireStudent(): array
    {
        $payload = $this->requireAuth();
        if ($payload['role'] !== 'student') {
            $this->error('Acceso restringido a estudiantes', 403);
        }
        return $payload;
    }
}
