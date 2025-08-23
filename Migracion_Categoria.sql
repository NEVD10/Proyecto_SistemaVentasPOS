CREATE TABLE Categoria (
    IdCategoria INT PRIMARY KEY IDENTITY(1,1),
    Nombre VARCHAR(30) NOT NULL
);

-- Crear tabla temporal para categorías únicas
CREATE TABLE #TempCategorias (
    Nombre VARCHAR(30) NOT NULL
);

-- Insertar categorías únicas de Producto
INSERT INTO #TempCategorias (Nombre)
SELECT DISTINCT Categoria FROM Producto WHERE Categoria IS NOT NULL;

-- Insertar en Categoria
INSERT INTO Categoria (Nombre)
SELECT Nombre FROM #TempCategorias;

-- Actualizar Producto con IdCategoria
UPDATE p
SET p.IdCategoria = c.IdCategoria
FROM Producto p
JOIN #TempCategorias tc ON p.Categoria = tc.Nombre
JOIN Categoria c ON tc.Nombre = c.Nombre;

-- Eliminar columna Categoria de Producto
ALTER TABLE Producto
DROP COLUMN Categoria;

-- Limpiar tabla temporal
DROP TABLE #TempCategorias;