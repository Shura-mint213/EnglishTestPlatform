using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Data.Entities
{
    /// <summary>
    /// Модель данных файла
    /// </summary>
    public class FileP
    {
        /// <summary>
        /// Id записи
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        /// <summary>
        /// Название файла
        /// </summary>
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Путь до файла
        /// </summary>
        [MaxLength(1024)]
        public string FilePath { get; set; } = string.Empty;
    }
}
