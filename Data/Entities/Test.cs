using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Data.Entities
{
    /// <summary>
    /// Модель данных теста
    /// </summary>
    public class Test 
    {
        /// <summary>
        /// ID записи 
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
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
        /// <summary>
        /// Раздел в котором находиться тест
        /// </summary>
        public int? SectionId { get; set; }
        /// <summary>
        /// Модель данных раздела
        /// </summary>
        [ForeignKey(nameof(SectionId))]
        public virtual Section? Section { get; set; }
    }
}
