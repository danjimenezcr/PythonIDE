// PyStudio — Patrón Decorator para la clase Script

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

// 1. INTERFAZ BASE: IScript
//    Define el contrato que todos los Scripts deben cumplir.
public interface IScript
{
    string GetRuta();
    string GetTexto();
}

// 2. CLASE CONCRETA BASE: Script
//    Implementación mínima: ruta y texto del archivo Python.
public class Script : IScript
{
    private readonly string _ruta;
    private readonly string _texto;

    public Script(string ruta)
    {
        _ruta  = ruta;
        _texto = File.ReadAllText(ruta);
    }

    public string GetRuta()  => _ruta;
    public string GetTexto() => _texto;
}

// 3. DECORADOR ABSTRACTO: ScriptDecorator
//    Envuelve un IScript y encarga las operaciones base.
//    Todos los decoradores concretos heredan de aquí.
public abstract class ScriptDecorator : IScript
{
    protected readonly IScript _script;

    protected ScriptDecorator(IScript script)
    {
        _script = script;
    }

    // Encarga al objeto envuelto por defecto
    public virtual string GetRuta()  => _script.GetRuta();
    public virtual string GetTexto() => _script.GetTexto();
}

// 4. DECORADOR CONCRETO: SignedScript (Script Firmado)
//    Añade firma digital HMAC-SHA256 sobre el contenido.
//    La firma se guarda en un archivo CSV local junto al script.
public class SignedScript : ScriptDecorator
{
    // Clave privada del cliente (debe coincidir con la del servidor)
    private const string ClavePrivada = "PYSTUDIO_SIGNATURE_KEY_2026";
    private readonly string _rutaFirma;

    public SignedScript(IScript script) : base(script)
    {
        // El CSV de firma se guarda junto al .py con extensión .sig.csv
        _rutaFirma = Path.ChangeExtension(_script.GetRuta(), ".sig.csv");
    }

    /// <summary>
    /// Genera la firma HMAC-SHA256 del contenido actual del script
    /// y la guarda en el archivo CSV local.
    /// </summary>
    public void GenerarFirma()
    {
        string firma = CalcularFirma(_script.GetTexto());
        // Guardar en CSV: ruta,firma,timestamp
        string linea = $"{_script.GetRuta()},{firma},{DateTime.UtcNow:O}";
        File.WriteAllText(_rutaFirma, linea);
        Console.WriteLine($"[SignedScript] Firma generada y guardada en: {_rutaFirma}");
    }

    /// <summary>
    /// Verifica si la firma guardada coincide con el contenido actual.
    /// Si el archivo fue modificado fuera de la app, retorna false.
    /// </summary>
    public bool VerificarFirma()
    {
        if (!File.Exists(_rutaFirma))
        {
            Console.WriteLine("[SignedScript] No existe firma guardada para este script.");
            return false;
        }

        string[] partes    = File.ReadAllText(_rutaFirma).Split(',');
        string firmaGuardada = partes[1];
        string firmaActual   = CalcularFirma(_script.GetTexto());

        bool esValida = firmaGuardada == firmaActual;
        Console.WriteLine(esValida
            ? "[SignedScript] ✓ Firma válida — el script no fue modificado externamente."
            : "[SignedScript] ✗ Firma inválida — el script fue modificado fuera de la aplicación.");

        return esValida;
    }

    /// <summary>
    /// Calcula HMAC-SHA256 del contenido usando la clave privada.
    /// </summary>
    private string CalcularFirma(string contenido)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ClavePrivada));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(contenido));
        return Convert.ToHexString(hash).ToLower();
    }
}

// 5. DECORADOR CONCRETO: FormattedScript (Script Formateado)
//    Añade syntax highlight básico para visualización en consola o en un RichTextBox de WinForms/WPF.
public class FormattedScript : ScriptDecorator
{
    // Palabras reservadas de Python
    private static readonly HashSet<string> Keywords = new()
    {
        "def", "class", "return", "if", "elif", "else", "for", "while", "import", "from", "as", "try", "except", "finally", "with", "pass", "break", "continue", "and", "or", "not", "in", "is", "True", "False", "None", "lambda", "yield", "raise", "del", "global", "nonlocal", "assert"
    };

    public FormattedScript(IScript script) : base(script) { }

    /// <summary>
    /// Devuelve el texto del script como lista de tokens con color asignado.
    /// Cada token indica: texto + color para renderizar en UI.
    /// </summary>
    public List<(string Texto, ConsoleColor Color)> GetTextoConSyntaxHighlight()
    {
        var tokens = new List<(string, ConsoleColor)>();
        string[] lineas = _script.GetTexto().Split('\n');

        foreach (string linea in lineas)
        {
            // Comentarios: toda la línea en verde
            string lineaTrim = linea.TrimStart();
            if (lineaTrim.StartsWith("#"))
            {
                tokens.Add((linea + "\n", ConsoleColor.Green));
                continue;
            }

            // Strings: texto entre comillas en amarillo
            if (lineaTrim.Contains('"') || lineaTrim.Contains('\''))
            {
                tokens.Add((linea + "\n", ConsoleColor.Yellow));
                continue;
            }

            // Palabras por palabra: keywords en azul, resto normal
            string[] palabras = linea.Split(' ');
            foreach (string palabra in palabras)
            {
                if (Keywords.Contains(palabra.Trim()))
                    tokens.Add((palabra + " ", ConsoleColor.Cyan));
                else
                    tokens.Add((palabra + " ", ConsoleColor.White));
            }
            tokens.Add(("\n", ConsoleColor.White));
        }

        return tokens;
    }

    /// <summary>
    /// Imprime el script con syntax highlight en la consola.
    /// En la app real esto alimentaría un RichTextBox de WinForms/WPF.
    /// </summary>
    public void ImprimirConColores()
    {
        foreach (var (texto, color) in GetTextoConSyntaxHighlight())
        {
            Console.ForegroundColor = color;
            Console.Write(texto);
        }
        Console.ResetColor();
    }
}

// 6. PROGRAMA PRINCIPAL: Demostración del patrón
class Program
{
    static void Main(string[] args)
    {
        // Crear un script de prueba temporal
        string rutaPrueba = "tarea1.py";
        File.WriteAllText(rutaPrueba,
            "# Tarea 1 - Variables\n" +
            "def saludar(nombre):\n" +
            "    return 'Hola ' + nombre\n" +
            "\n" +
            "resultado = saludar('María')\n" +
            "print(resultado)\n"
        );

        Console.WriteLine("=== PyStudio — Patrón Decorator ===\n");

        // --- Script base ---
        Console.WriteLine("--- 1. Script base ---");
        IScript scriptBase = new Script(rutaPrueba);
        Console.WriteLine($"Ruta:  {scriptBase.GetRuta()}");
        Console.WriteLine($"Texto:\n{scriptBase.GetTexto()}");

        // --- Script Firmado ---
        Console.WriteLine("\n--- 2. Script Firmado ---");
        SignedScript scriptFirmado = new SignedScript(new Script(rutaPrueba));
        scriptFirmado.GenerarFirma();
        scriptFirmado.VerificarFirma();   // debe ser válida

        // Simular modificación externa
        Console.WriteLine("\n[Simulando modificación externa del archivo...]");
        File.AppendAllText(rutaPrueba, "\n# línea agregada externamente\n");

        SignedScript scriptModificado = new SignedScript(new Script(rutaPrueba));
        scriptModificado.VerificarFirma();  // debe detectar alteración

        // --- Script Formateado ---
        Console.WriteLine("\n--- 3. Script con Syntax Highlight ---");
        // Restaurar archivo original
        File.WriteAllText(rutaPrueba,
            "# Tarea 1 - Variables\n" +
            "def saludar(nombre):\n" +
            "    return 'Hola ' + nombre\n" +
            "resultado = saludar('María')\n" +
            "print(resultado)\n"
        );
        FormattedScript scriptFormateado = new FormattedScript(new Script(rutaPrueba));
        scriptFormateado.ImprimirConColores();

        // --- Script Firmado + Formateado (decoradores combinados) ---
        Console.WriteLine("\n--- 4. Script Firmado + Formateado (combinado) ---");
        FormattedScript scriptCompleto =
            new FormattedScript(
                new SignedScript(
                    new Script(rutaPrueba)
                )
            );
        Console.WriteLine($"Ruta: {scriptCompleto.GetRuta()}");
        scriptCompleto.ImprimirConColores();

        // Limpiar archivos de prueba
        File.Delete(rutaPrueba);
        File.Delete(Path.ChangeExtension(rutaPrueba, ".sig.csv"));
    }
}
