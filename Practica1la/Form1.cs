using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;

namespace Practica1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            analizarToolStripMenuItem.Enabled = false; // Desactiva la opción de análisis al inicio
        }

        // Maneja el evento de crear un nuevo archivo
        private void NuevoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear(); // Limpia el contenido del RichTextBox
            archivo = null; // Resetea la variable archivo
        }

        // Maneja el evento de abrir un archivo existente
        private void AbrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog
            {
                Filter = "Texto|*.c" // Filtra para mostrar solo archivos .c
            };
            if (VentanaAbrir.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaAbrir.FileName; // Asigna el archivo seleccionado

                using (StreamReader Leer = new StreamReader(archivo))
                {
                    richTextBox1.Text = Leer.ReadToEnd(); // Lee el contenido y lo muestra
                }
                Form1.ActiveForm.Text = "Mi compilador -" + archivo; // Actualiza el título del formulario
            }
            analizarToolStripMenuItem.Enabled = true; // Activa el botón de análisis
        }

        // Maneja el evento de guardar el archivo
        private void GuardarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            guardar(); // Llama al método de guardar
        }

        private void guardar()
        {
            throw new NotImplementedException();
        }

        // Método para guardar el contenido del RichTextBox
        private void Guardar()
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog
            {
                Filter = "Texto|*.c" // Filtra para guardar solo como .c
            };
            if (archivo != null)
            {
                using (StreamWriter Escribir = new StreamWriter(archivo))
                {
                    Escribir.Write(richTextBox1.Text); // Escribe el texto en el archivo
                }
            }
            else
            {
                if (VentanaGuardar.ShowDialog() == DialogResult.OK)
                {
                    archivo = VentanaGuardar.FileName;
                    using (StreamWriter Escribir = new StreamWriter(archivo))
                    {
                        Escribir.Write(richTextBox1.Text); // Escribe el texto en el nuevo archivo
                    }
                }
            }
            Form1.ActiveForm.Text = "Mi compilador -" + archivo; // Actualiza el título del formulario
        }

        // Maneja el evento de guardar como
        private void GuardarComoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog();

            if (VentanaGuardar.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaGuardar.FileName;
                using (StreamWriter Escribir = new StreamWriter(archivo))
                {
                    Escribir.Write(richTextBox1.Text); // Escribe el texto en el nuevo archivo
                }
            }
        }

        // Maneja el evento de salir de la aplicación
        private void SalirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit(); // Cierra la aplicación
        }

        // Determina el tipo de carácter recibido
        private char Tipo_caracter(int caracter)
        {
            // Verifica si es una letra
            if (caracter >= 65 && caracter <= 90 || caracter >= 97 && caracter <= 122)
                return 'l'; // Letra
            else if (caracter >= 48 && caracter <= 57)
                return 'd'; // Dígito
            else
            {
                // Caracteres especiales
                switch (caracter)
                {
                    case 10: return 'n'; // Nueva línea
                    case 34: return '"'; // Comillas dobles
                    case 39: return 'c'; // Comilla simple
                    case 32: return 'e'; // Espacio
                    case 47: return '/';//inicio de comentario de linea o de bloque
                    default: return 's'; // Símbolo
                };
            }
        }

        // Maneja la lectura de símbolos
        private void Simbolo()
        {
            if (IsValidSymbol(i_caracter))
            {
                elemento = $"{(char)i_caracter}\n"; // Almacena el símbolo
            }
            else
            {
                Error(i_caracter); // Maneja el error si el símbolo no es válido
            }
        }

        // Verifica si el símbolo es válido
        private bool IsValidSymbol(int caracter)
        {
            return caracter == 33 || caracter == 35 ||
                   (caracter >= 37 && caracter <= 40) ||
                   (caracter >= 40 && caracter <= 45) ||
                   caracter == 47 ||
                   (caracter >= 58 && caracter <= 62) ||
                   caracter == 91 || caracter == 93 || caracter == 94 ||
                   caracter == 123 || caracter == 124 || caracter == 125;
        }

        // Maneja la lectura de cadenas
        private void Cadena()
        {
            do
            {
                i_caracter = Leer.Read(); // Lee carácter por carácter
                if (i_caracter == 10) Numero_linea++; // Incrementa el número de línea si encuentra nueva línea
            } while (i_caracter != 34 && i_caracter != -1); // Continua hasta encontrar el cierre de la cadena o EOF
            if (i_caracter == -1) Error(-1); // Maneja error si llega al final del archivo
        }

        // Maneja la lectura de caracteres
        private void Caracter()
        {
            i_caracter = Leer.Read(); // Lee el primer carácter
            i_caracter = Leer.Read(); // Lee el segundo carácter
            if (i_caracter != 39) Error(39); // Verifica que sea una comilla simple
        }

        // Maneja errores léxicos
        private void Error(int i_caracter)
        {
            Rtbx_salida.AppendText("Error léxico " + i_caracter + ", línea " + Numero_linea + "\n");
            N_error++; // Incrementa el contador de errores
        }

        // Maneja la lectura de identificadores
        private int Identificador()
        {
            do
            {
                elemento += (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'l' || Tipo_caracter(i_caracter) == 'd');
            if (PalabraReservada(out int token))
            {
                elemento += "\n";
                return token;
            }
            else
            {
                switch (i_caracter)
                {
                    case '.':
                        Archivo_Libreria();
                        return 510;
                    case '(':
                        if (elemento == "main")
                        {
                            elemento = "main\n";
                        } else
                            elemento = "funcion\n";
                        return 520;
                    case '>':
                        elemento = "condicion\n";
                        return 560;
                    default:
                        elemento = "identificador\n";
                        return 500;
                }
            }
        }

        // Verifica si un identificador es una palabra reservada
        private bool PalabraReservada(out int token)
        {
            string palabra = elemento.Trim();
            return P_Reservadas.TryGetValue(palabra, out token);
        }

        // Maneja la lectura de archivos de librería
        private void Archivo_Libreria()
        {
            elemento = elemento + (char)i_caracter;
            i_caracter = Leer.Read();
            if ((char)i_caracter == 'h')
            {
                elemento = "Libreria\n";
                i_caracter = Leer.Read();
            }
            else
            {
                Error(i_caracter);
            }
        }

        private void Numero_Real()
        {
            do
            {
                i_caracter = Leer.Read(); // Lee el siguiente carácter
            } while (Tipo_caracter(i_caracter) == 'd'); // Continua mientras sea un dígito
            elemento = "numero_real\n"; // Marca como número real
        }

        // Maneja la lectura de números enteros
        private void Numero()
        {
            do
            {
                i_caracter = Leer.Read(); // Lee el siguiente carácter
            } while (Tipo_caracter(i_caracter) == 'd'); // Continua mientras sea un dígito
            if ((char)i_caracter == '.')
            {
                Numero_Real(); // Si hay un punto, maneja como número real
            }
            else
            {
                elemento = "numero_entero\n"; // Marca como número entero
            }
        }
        private bool Comentario()
        {
            i_caracter = Leer.Read();
            switch (i_caracter)
            {
                case 47:
                    do
                    {
                        i_caracter = Leer.Read();
                    } while (i_caracter != 10);
                    return true;
                case 42:
                    do
                    {
                        do
                        {
                            i_caracter = Leer.Read();
                            if (i_caracter == 10) { Numero_linea++; }
                        } while (i_caracter != 42 && i_caracter != -1);
                        i_caracter = Leer.Read();
                    } while (i_caracter != 47 && i_caracter != -1);
                    if (i_caracter == -1) { Error(i_caracter); }
                    i_caracter = Leer.Read();
                    return true;
                default: return false;
            }
        }


        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            analizarToolStripMenuItem.Enabled = true;
        }

        private void Rtbx_salida_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }


        private void AnalizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Rtbx_salida.Clear();
            guardar();
            N_error = 0;
            Numero_linea = 1;

            archivoback = Path.ChangeExtension(archivo, "back");
            using (Escribir = new StreamWriter(archivoback))
            using (StreamWriter escribirTok = new StreamWriter(Path.ChangeExtension(archivo, "tok")))
            using (Leer = new StreamReader(archivo))
            {
                i_caracter = Leer.Read();
                while (i_caracter != -1)
                {
                    elemento = "";
                    int token = -1;

                    switch (Tipo_caracter(i_caracter))
                    {
                        case 'l':
                            token = Identificador();
                            Escribir.Write(elemento);
                            break;

                        case 'd':
                            Numero();
                            Escribir.Write(elemento);
                            token = 501;
                            break;

                        case 's':
                            Simbolo();
                            Escribir.Write(elemento);
                            escribirTok.WriteLine($"{(int)i_caracter}");
                            i_caracter = Leer.Read();
                            break;

                        case '"':
                            Cadena();
                            Escribir.Write("cadena\n");
                            token = 504;
                            i_caracter = Leer.Read();
                            break;

                        case 'c':
                            Caracter();
                            Escribir.Write("caracter\n");
                            token = 505;
                            i_caracter = Leer.Read();
                            break;

                        case 'n':
                            Escribir.Write("salto de linea\n");
                            i_caracter = Leer.Read();
                            Numero_linea++;
                            token = 506;
                            break;

                        case '/':
                            if (Comentario())
                            {
                                Escribir.Write("comentario\n");
                            }
                            else
                            {
                                Escribir.Write("/\n");
                            }
                            token = 508;
                            break;

                        case 'e':
                            Escribir.Write("espacio\n");
                            i_caracter = Leer.Read();
                            token = 507;
                            break;
                    }

                    if (token != -1)
                    {
                        escribirTok.WriteLine($"{token}");
                    }
                    if (i_caracter == -1)
                    {
                        Escribir.Write("Fin");
                    }
                }
            }

            if (N_error == 0) { Rtbx_salida.AppendText("Errores Lexicos: " + N_error); A_Sintactico(); }
            else { Rtbx_salida.AppendText("Errores: " + N_error); }
        }

        private void ErrorS(string e, string s)
        {
            Rtbx_salida.AppendText("Linea: " + Numero_linea + ". Error de sintaxis " + e + ", se esperaba " + s + "\n");
            token = ""; N_error++;
        }

        private void A_Sintactico()
        {
            Rtbx_salida.AppendText("\nAnalizando sintaxis...\n");
            N_error = 0; Numero_linea = 1;
            Leer = new StreamReader(archivoback);
            if (Cabecera() == 1)
            {
                F_Main();
            }
            else
            {
                ErrorS(token, "funcion main()");
            }
            Rtbx_salida.AppendText("Errores sintácticos: " + N_error);
            Leer.Close();
        }

        private void F_Main()
        {
            token = Leer.ReadLine();

            if (token == null)
            {
                ErrorS("fin de archivo", "(");
                return;
            }

            if (token == "(")
            {
                token = Leer.ReadLine();

                if (token == null)
                {
                    ErrorS("fin de archivo", ")");
                    return;
                }

                if (token == ")")
                {
                    token = Leer.ReadLine();

                    while (token == "espacio" || token == "salto de linea" || token == "comentario")
                    {
                        token = Leer.ReadLine();
                    }
                    if (token == null)
                    {
                        while (token == "espacio" || token == "salto de linea" || token == "comentario")
                        {
                            token = Leer.ReadLine();
                        }
                        ErrorS("fin de archivo", "{");
                        return;
                    }

                    Bloque_Sentencias();
                }
                else
                {
                    ErrorS(token, ")");
                }
            }
            else
            {
                ErrorS(token, "(");
            }
        }

        private int Bloque_Sentencias()
        {
            if (token == "{")
            {
                while (true)
                {
                    token = Leer.ReadLine();

                    if (token == null)
                    {
                        token = Leer.ReadLine();

                        ErrorS("fin de archivo", "se esperaba '}' al final del bloque.");
                        return 0;
                    }
                    else
                    {
                        while (token == "espacio" || token == "salto de linea" || token == "comentario")
                        {
                            token = Leer.ReadLine();
                        }
                    }

                    switch (token)
                    {
                        case "}":
                            return 1;

                        case "{":
                            Bloque_Sentencias();
                            break;

                        case "salto de linea":
                            Numero_linea++;
                            token = Leer.ReadLine();
                            break;

                        default:
                            Sentencia();
                            break;
                    }
                }
            }
            else
            {
                ErrorS(token, "Error: se esperaba '{' al inicio del bloque de sentencias.");
            }

            return 0;
        }


        private void Sentencia()
        {
            if (token == null)
            {
                ErrorS("fin de archivo", "una sentencia válida");
                return;
            }

            switch (token)
            {
                case ";":
                    token = Leer.ReadLine();
                    break;
                case "espacio":
                    break;
                case "if":
                    E_if();
                    break;
                case "else":
                    token = Leer.ReadLine();
                    break;
                case "do":
                    E_do();
                    break;
                case "for":
                    E_for();
                    break;
                case "while":
                    E_while();
                    break;
                case "switch":
                    E_switch();
                    break;
                case "identificador":
                    asignacion();
                    break;
                case "funcion":
                    Llamada_funcion();
                    break;
                case "comentario":
                    token = Leer.ReadLine();
                    break;
                default:
                    ErrorS(token, "una sentencia válida");
                    break;
            }
        }

        private void asignacion()
        {
            throw new NotImplementedException();
        }

        private void Llamada_funcion()
        {
            token = Leer.ReadLine();
            switch (token)
            {
                case "(":
                    token = Leer.ReadLine();
                    if (token == "cadena")
                    {
                        token = Leer.ReadLine();
                        if (token == ")")
                        {
                            token = Leer.ReadLine();
                        }
                        else
                        {
                            ErrorS(token, "Error: se esperaba ')'");
                            N_error++;
                        }
                    }
                    else
                    {
                        ErrorS(token, "Error: se esperaba 'cadena' después de '('");
                        N_error++;
                    }
                    break;
                default:
                    ErrorS(token, "Error: se esperaba '(' al inicio de la llamada a función");
                    N_error++;
                    break;
            }
        }



        private void Asignacion()
        {
            token = Leer.ReadLine();
            if (token == "identificador")
            {
                token = Leer.ReadLine();
                if (token == "=")
                {
                    token = Leer.ReadLine();
                    E_expresion();

                    if (token == ";")
                    {
                        token = Leer.ReadLine();
                        while (token == "espacio" || token == "salto de linea" || token == "comentario")
                        {
                            token = Leer.ReadLine();
                        }
                    }
                    else
                    {
                        ErrorS(token, "se esperaba ';' al final de la asignación.");
                        N_error++;
                    }
                }
                else
                {
                    ErrorS(token, "se esperaba '=' para la asignación.");
                    N_error++;
                }
            }
            else
            {
                ErrorS(token, "se esperaba un identificador.");
                N_error++;
            }
        }

        private void E_expresion()
        {
            E_termino();

            while (token == "+" || token == "-")
            {
                string operador = token;
                token = Leer.ReadLine();
                E_termino();
            }
        }

        private void E_termino()
        {
            E_factor();

            while (token == "*" || token == "/")
            {
                string operador = token;
                token = Leer.ReadLine();
                E_factor();
            }
        }
        private void E_factor()
        {
            if (token == "numero" || token == "identificador")
            {
                token = Leer.ReadLine();
            }
            else if (token == "(")
            {
                token = Leer.ReadLine();
                E_expresion();

                if (token == ")")
                {
                    token = Leer.ReadLine();
                }
                else
                {
                    ErrorS(token, "se esperaba ')'.");
                    N_error++;
                }
            }
            else
            {
                ErrorS(token, "se esperaba un número, un identificador o una expresión entre paréntesis.");
                N_error++;
            }
        }


        private void E_switch()
        {
            token = Leer.ReadLine();
            if (token == "(")
            {
                token = Leer.ReadLine();
                E_expresion();

                if (token == ")")
                {
                    token = Leer.ReadLine();
                    while (token == "espacio" || token == "salto de linea" || token == "comentario")
                    {
                        token = Leer.ReadLine();
                    }

                    if (token == "{")
                    {
                        token = Leer.ReadLine();
                        while (token == "espacio" || token == "salto de linea" || token=="comentario")
                        {
                            token = Leer.ReadLine();
                        }

                        while (token == "case" || token == "default")
                        {
                            if (token == "case")
                            {
                                token = Leer.ReadLine();
                                Constante();

                                if (token == ":")
                                {
                                    token = Leer.ReadLine();
                                    Bloque_Sentencias();

                                    if (token == "break")
                                    {
                                        token = Leer.ReadLine();
                                        if (token == ";")
                                        {
                                            token = Leer.ReadLine();
                                        }
                                        else
                                        {
                                            ErrorS(token, "se esperaba ';' después de 'break'.");
                                            N_error++;
                                        }
                                    }
                                    else
                                    {
                                        ErrorS(token, "se esperaba 'break' al final del case.");
                                        N_error++;
                                    }
                                }
                                else
                                {
                                    ErrorS(token, "se esperaba ':' después del valor del case.");
                                    N_error++;
                                }
                            }
                            else if (token == "default")
                            {
                                token = Leer.ReadLine();
                                if (token == ":")
                                {
                                    token = Leer.ReadLine();
                                    Bloque_Sentencias();

                                    if (token == "break")
                                    {
                                        token = Leer.ReadLine();
                                        if (token == ";")
                                        {
                                            token = Leer.ReadLine();
                                        }
                                        else
                                        {
                                            ErrorS(token, "se esperaba ';' después de 'break' en default.");
                                            N_error++;
                                        }
                                    }
                                }
                                else
                                {
                                    ErrorS(token, "se esperaba ':' después de default.");
                                    N_error++;
                                }
                            }

                            while (token == "espacio" || token == "salto de linea")
                            {
                                token = Leer.ReadLine();
                            }
                        }

                        if (token == "}")
                        {
                            token = Leer.ReadLine();
                            while (token == "espacio" || token == "salto de linea" || token == "comentario")
                            {
                                token = Leer.ReadLine();
                            }
                        }
                        else
                        {
                            ErrorS(token, "se esperaba '}' al final del bloque switch.");
                            N_error++;
                        }
                    }
                    else
                    {
                        ErrorS(token, "se esperaba '{' al inicio del bloque switch.");
                        N_error++;
                    }
                }
                else
                {
                    ErrorS(token, "se esperaba ')' después de la expresión de control del switch.");
                    N_error++;
                }
            }
            else
            {
                ErrorS(token, "se esperaba '(' después de 'switch'.");
                N_error++;
            }
        }


        private void E_while()
        {
            token = Leer.ReadLine();
            if (token == "(")
            {

                E_condicion();

                if (token == ")")
                {
                    token = Leer.ReadLine();
                    while (token == "espacio" || token == "salto de linea" || token == "comentario")
                    {
                        token = Leer.ReadLine();
                    }

                    if (token == "{")
                    {
                        Bloque_Sentencias();

                        if (token == "}")
                        {
                            while (token == "espacio" || token == "salto de linea" || token == "comentario")
                            {
                                token = Leer.ReadLine();
                            }
                        }
                    }
                    else
                    {
                        ErrorS(token, "se esperaba '{' al inicio del bloque de instrucciones.");
                        N_error++;
                    }
                }
                else
                {
                    ErrorS(token, "se esperaba ')'.");
                    N_error++;
                }
            }
            else
            {
                ErrorS(token, "se esperaba '('.");
                N_error++;
            }
        }
            private void E_for()
            {
                token = Leer.ReadLine();
                if (token == "(")
                {
                    token = Leer.ReadLine();
                    // Manejo de la inicialización (por ejemplo, int i = 0;)
                    if (token == "identificador")
                    {
                        asignacion(); // Llama al método de asignación para manejar la inicialización
                    }
                    else
                    {
                        ErrorS(token, "se esperaba una inicialización en el ciclo for.");
                        N_error++;
                    }

                    // Verificación de la condición
                    if (token == ";")
                    {
                        token = Leer.ReadLine();
                        E_condicion(); // Llama al método que maneja las condiciones

                        if (token == ";")
                        {
                            token = Leer.ReadLine();
                            // Manejo del incremento o actualización
                            if (token == "identificador")
                            {
                                asignacion(); // Llama al método de asignación para manejar el incremento
                            }
                            else
                            {
                                ErrorS(token, "se esperaba una expresión de incremento en el ciclo for.");
                                N_error++;
                            }

                            if (token == ")")
                            {
                                token = Leer.ReadLine();
                            while (token == "espacio" || token == "salto de linea" || token == "comentario")
                            {
                                    token = Leer.ReadLine();
                                }

                                // Bloque de instrucciones del ciclo for
                                if (token == "{")
                                {
                                    Bloque_Sentencias();

                                    if (token == "}")
                                    {
                                    while (token == "espacio" || token == "salto de linea" || token == "comentario")
                                    {
                                            token = Leer.ReadLine();
                                        }
                                    }
                                    else
                                    {
                                        ErrorS(token, "se esperaba '}' al final del bloque de instrucciones del ciclo for.");
                                        N_error++;
                                    }
                                }
                                else
                                {
                                    ErrorS(token, "se esperaba '{' para el bloque de instrucciones del ciclo for.");
                                    N_error++;
                                }
                            }
                            else
                            {
                                ErrorS(token, "se esperaba ')' después de la expresión de incremento.");
                                N_error++;
                            }
                        }
                        else
                        {
                            ErrorS(token, "se esperaba ';' después de la condición en el ciclo for.");
                            N_error++;
                        }
                    }
                    else
                    {
                        ErrorS(token, "se esperaba ';' después de la inicialización en el ciclo for.");
                        N_error++;
                    }
                }
                else
                {
                    ErrorS(token, "se esperaba '(' después de 'for'.");
                    N_error++;
                }
            }


        private void E_do()
        {
            token = Leer.ReadLine();

            if (token == "{")
            {
                Bloque_Sentencias();

                if (token == "}")
                {
                    token = Leer.ReadLine();
                    while (token == "espacio" || token == "salto de linea" || token == "comentario")
                    {
                        token = Leer.ReadLine();
                    }

                    if (token == "while")
                    {
                        token = Leer.ReadLine();
                        if (token == "(")
                        {
                            E_condicion();

                            if (token == ")")
                            {
                                token = Leer.ReadLine();
                                while (token == "espacio" || token == "salto de linea" || token == "comentario")
                                {
                                    token = Leer.ReadLine();
                                }
                            }
                            else
                            {
                                ErrorS(token, "se esperaba ')'.");
                                N_error++;
                            }
                        }
                        else
                        {
                            ErrorS(token, "se esperaba '(' después de 'while'.");
                            N_error++;
                        }
                    }
                    else
                    {
                        ErrorS(token, "se esperaba 'while' después del bloque 'do'.");
                        N_error++;
                    }
                }
                else
                {
                    ErrorS(token, "se esperaba '}' al final del bloque de instrucciones.");
                    N_error++;
                }
            }
            else
            {
                ErrorS(token, "se esperaba '{' al inicio del bloque de instrucciones.");
                N_error++;
            }
        }


        private void E_if()
        {
            token = Leer.ReadLine();
            if (token == "(")
            {
                E_condicion();

                if (token == ")")
                {
                    token = Leer.ReadLine();
                    while (token == "espacio" || token == "salto de linea" || token == "comentario")
                    {
                        token = Leer.ReadLine();
                    }

                    if (token == "{")
                    {
                        Bloque_Sentencias();

                        if (token == "}")
                        {
                            while (token == "espacio" || token == "salto de linea" || token == "comentario")
                            {
                                token = Leer.ReadLine();
                            }
                            if (token == "else")
                            {
                                while (token == "espacio" || token == "salto de linea" || token == "comentario")
                                {
                                    token = Leer.ReadLine();
                                }

                                if (token == "{")
                                {
                                    Bloque_Sentencias();

                                    if (token == "}")
                                    {
                                        while (token == "espacio" || token == "salto de linea" || token == "comentario")
                                        while (token == "espacio" || token == "salto de linea" || token == "comentario")
                                        {
                                            token = Leer.ReadLine();
                                        }
                                    }
                                    else
                                    {
                                        ErrorS(token, "se esperaba '}' al final del bloque else.");
                                        N_error++;
                                    }
                                }
                                else
                                {
                                    ErrorS(token, "se esperaba '{' al inicio del bloque else.");
                                    N_error++;
                                }
                            }
                        }
                        else
                        {
                            ErrorS(token, "se esperaba '}' al final del bloque if.");
                            N_error++;
                        }
                    }
                    else
                    {
                        ErrorS(token, "se esperaba '{' al inicio del bloque de instrucciones.");
                        N_error++;
                    }
                }
                else
                {
                    ErrorS(token, "se esperaba ')'.");
                    N_error++;
                }
            }
            else
            {
                ErrorS(token, "se esperaba '('.");
                N_error++;
            }
        }


        private void E_condicion()
        {
            token = Leer.ReadLine();
            if (token == "<")
            {
                token = Leer.ReadLine();
                if (token == "condicion")
                {
                    token = Leer.ReadLine();
                    if (token == ">")
                    {
                        token = Leer.ReadLine();
                    }
                }
            }
        }

        private void Declaracion()
        {
            switch (token)
            {
                case "identificador":
                    Dec_VGlobal();
                    break;
                case "funcion":
                    Dec_Funcion();
                    break;
                default:
                    ErrorS(token, "declaracion global válida");
                    break;
            }
        }

        private void Dec_Funcion()
        {
            token = Leer.ReadLine();
            switch (token)
            {
                case "(":
                    token = Leer.ReadLine();
                    if (token == "tipo")
                    {
                        token = Leer.ReadLine();
                        if (token == ")")
                        {
                            token = Leer.ReadLine();
                        }
                        else
                        {
                            ErrorS(token, "Error: se esperaba ')'");
                            N_error++;
                        }
                    }
                    else
                    {
                        ErrorS(token, "Error: se esperaba 'cadena' después de '('");
                        N_error++;
                    }
                    break;
                default:
                    ErrorS(token, "Error: se esperaba '(' al inicio de la llamada a función");
                    N_error++;
                    break;
            }
        }

        private int Constante()
        {
            token = Leer.ReadLine();
            switch (token)
            {
                case "numero_real": return 1;
                case "numero_entero": return 1;
                case "caracter": return 1;
                case "identificador": return 1;
                default: return 0;
            }
        }

        private void D_Arreglos()
        {
            while (token == "[")
            {
                token = Leer.ReadLine();
                if (token == "identificador" || token == "numero_entero")
                {
                    token = Leer.ReadLine();
                    if (token == "]")
                    {
                        token = Leer.ReadLine();
                    }
                    else
                    {
                        ErrorS(token, "]");
                    }
                }
                else
                {
                    ErrorS(token, "valor de longitud");
                }
            }
            switch (token)
            {
                case ";":
                    token = Leer.ReadLine();
                    break;
                case "=":
                    token = Leer.ReadLine();
                    if (token == "{")
                    {
                        Bloque_Inicializacion();
                        token = Leer.ReadLine();
                        if (token == "}")
                        {
                            token = Leer.ReadLine();
                            if (token == ";")
                            {
                                token = Leer.ReadLine();
                            }
                            else
                            {
                                ErrorS(token, ";");
                            }
                        }
                        else
                        {
                            ErrorS(token, "}");
                        }
                    }
                    else
                    {
                        ErrorS(token, "{");
                    }
                    break;
                default:
                    ErrorS(token, "declaración válida para arreglos.");
                    break;
            }
        }

        private void Bloque_Inicializacion()
        {
            do
            {
                token = Leer.ReadLine();
                if (token == "{")
                {
                    do
                    {
                        if (Constante() == 1)
                        {
                            token = "elemento";
                        }
                        switch (token)
                        {
                            case "elemento":
                                token = Leer.ReadLine();
                                break;
                            case "{":
                                do
                                {
                                    if (Constante() == 0)
                                    {
                                        ErrorS(token, "inicialización válida de arreglo.");
                                    }
                                    else
                                    {
                                        token = Leer.ReadLine();
                                    }
                                } while (token == ",");
                                if (token == "}")
                                {
                                    token = Leer.ReadLine();
                                }
                                else
                                {
                                    ErrorS(token, "}");
                                }
                                break;
                        }
                    } while (token == ",");
                    if (token == "}")
                    {
                        token = Leer.ReadLine();
                    }
                    else
                    {
                        ErrorS(token, "}");
                    }
                }
                else
                {
                    ErrorS(token, "{");
                }
            } while (token == ",");
        }

        private void Dec_VGlobal()
        {
            token = Leer.ReadLine();
            switch (token)
            {
                case "=":
                    if (Constante() == 1)
                    {
                        token = Leer.ReadLine();
                        if (token == ";") { token = Leer.ReadLine(); }
                        else { ErrorS(token, ";"); }
                    }
                    else { ErrorS(token, "inicializacion global valida"); }
                    break;
                case "[": D_Arreglos(); break;
                case ";": token = Leer.ReadLine(); break;
                default: ErrorS(token, ";"); break;
            }
        }

        private void Directriz()
        {
            token = Leer.ReadLine();
            switch (token)
            {
                case "include":
                    Include();
                    break;
                case "define":
                    break;
                case "if":
                    break;
                case "error":
                    break;
                default:
                    ErrorS(token, "directriz de procesador");
                    break;
            }
        }
        private void Include()
        {
            token = Leer.ReadLine();
            if (token == "<")
            {
                token = Leer.ReadLine();
                if (token == "Libreria")
                {
                    token = Leer.ReadLine();
                    if (token == ">")
                    {
                        token = Leer.ReadLine();
                    }
                    else
                    {
                        ErrorS(token, ">");
                        N_error++;
                    }
                }
                else
                {
                    ErrorS(token, "nombre de archivo de librería estándar");
                    N_error++;
                }
            }
            else if (token == "cadena")
            {
                token = Leer.ReadLine();
        }
            }

        private int Cabecera()
        {
            token = Leer.ReadLine();
            do
            {
                if (P_Res_Tipo.IndexOf(token) >= 0)
                {
                    token = "tipo";
                }
                switch (token)
                {
                    case "#":
                        Directriz();
                        break;
                    case "espacio":
                        token = Leer.ReadLine();
                        break;
                    case "tipo":
                        token = Leer.ReadLine();
                        while (token == "espacio")
                        {
                            token = Leer.ReadLine();
                        }
                        if (token == "main") return 1;
                        else Declaracion();
                        break;
                    case "comentario":
                        token = Leer.ReadLine();
                        break;
                    case "salto de linea":
                        token = Leer.ReadLine();
                        break;
                    default:
                        token = Leer.ReadLine();
                        break;
                }
            } while (token != "Fin" && token != "main");
            return 0;
        }
    }
}
