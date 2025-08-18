// Models/Cliente.cs
using System;
using System.ComponentModel.DataAnnotations;

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
    }
}