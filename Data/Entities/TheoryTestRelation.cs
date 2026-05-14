using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Data.Entities
{
    /// <summary>
    /// Модель связки теории и тестов
    /// </summary>
    public class TheoryTestRelation
    {
        /// <summary>
        /// ID записи
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        /// <summary>
        /// ID записи теста
        /// </summary>
        public int TestId { get; set; }
        /// <summary>
        /// Модель данных теста
        /// </summary>
        public Test Test { get; set; } = null!;
        /// <summary>
        /// ID записи теории
        /// </summary>
        public int TheoryId { get; set; }
        /// <summary>
        /// Модель данных теории
        /// </summary>
        public Theory Theory { get; set; } = null!;
    }
}
