using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Data.Entities
{
    /// <summary>
    /// Модель данных реории
    /// </summary>
    public class Theory
    {
        /// <summary>
        /// ID записи
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        /// <summary>
        /// Название теории
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// ID записи файла
        /// </summary>
        public int FileId { get; set; }
        /// <summary>
        /// Модель данных файла
        /// </summary>
        public FileP File { get; set; } = null!;
        /// <summary>
        /// Коллекция связок теории и тестов
        /// </summary>
        public ICollection<TheoryTestRelation> TheoryTestRelations { get; set; } = new List<TheoryTestRelation>();
    }
}
