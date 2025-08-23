using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace SistemaVentas.Models
{
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "El tipo de documento es requerido.")]
        [StringLength(15, ErrorMessage = "El tipo de documento no puede exceder los 15 caracteres.")]
        public string TipoDocumento { get; set; }

        [Required(ErrorMessage = "El número de documento es requerido.")]
        [StringLength(12, ErrorMessage = "El número de documento no puede exceder los 12 caracteres.")]
        [CustomValidation(typeof(Cliente), "ValidarNumeroDocumento", ErrorMessage = "El número de documento no cumple con el formato requerido para el tipo de documento seleccionado.")]
        public string NumeroDocumento { get; set; }

        [Required(ErrorMessage = "Los nombres son requeridos.")]
        [StringLength(30, ErrorMessage = "Los nombres no pueden exceder los 30 caracteres.")]
        public string Nombres { get; set; }

        [StringLength(60, ErrorMessage = "Los apellidos no pueden exceder los 60 caracteres.")]
        public string Apellidos { get; set; }

        [StringLength(9, ErrorMessage = "El teléfono no puede exceder los 9 caracteres.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "El teléfono debe tener exactamente 9 dígitos.")]
        public string Telefono { get; set; }

        [StringLength(100, ErrorMessage = "El email no puede exceder los 100 caracteres.")]
        [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
        public string Email { get; set; }

        [StringLength(100, ErrorMessage = "La dirección no puede exceder los 100 caracteres.")]
        public string Direccion { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Método de validación personalizado
        public static ValidationResult ValidarNumeroDocumento(string numeroDocumento, ValidationContext context)
        {
            var cliente = (Cliente)context.ObjectInstance;
            if (string.IsNullOrEmpty(numeroDocumento))
                return ValidationResult.Success; // Esto se maneja con [Required]

            if (cliente.TipoDocumento == "RUC" && (!Regex.IsMatch(numeroDocumento, @"^\d{11}$")))
                return new ValidationResult("El RUC debe tener exactamente 11 dígitos.");

            if (cliente.TipoDocumento == "Carnet de Extranjería" && (!Regex.IsMatch(numeroDocumento, @"^\d{8,12}$")))
                return new ValidationResult("El Carnet de Extranjería debe tener entre 8 y 12 dígitos.");

            if (cliente.TipoDocumento == "DNI" && (!Regex.IsMatch(numeroDocumento, @"^\d{8}$")))
                return new ValidationResult("El DNI debe tener exactamente 8 dígitos.");

            return ValidationResult.Success;
        }
    }
}