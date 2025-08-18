namespace SistemaVentas.Models
{
    public class Paginador<T>
    {
        public IEnumerable<T> Elementos { get; set; }
        public int TotalRegistros { get; set; }
        public int NumeroPagina { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / TamanoPagina);
        public bool TienePaginaAnterior => NumeroPagina > 1;
        public bool TienePaginaSiguiente => NumeroPagina < TotalPaginas;
    }
}