using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    public class Section
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>
        /// Родительский раздел (nullable для корневых разделов)
        /// </summary>
        public int? ParentSectionId { get; set; }

        [ForeignKey(nameof(ParentSectionId))]
        public virtual Section? ParentSection { get; set; }

        /// <summary>
        /// Дочерние разделы
        /// </summary>
        public virtual ICollection<Section> ChildSections { get; set; } = new List<Section>();

        /// <summary>
        /// Навигационные свойства для теорий, принадлежащих разделу
        /// </summary>
        public virtual ICollection<Theory> Theories { get; set; } = new List<Theory>();
        /// <summary>
        /// Навигационные свойства для тестов, принадлежащих разделу
        /// </summary>
        public virtual ICollection<Test> Tests { get; set; } = new List<Test>();

        /// <summary>
        /// Порядок сортировки (опционально)
        /// </summary>
        public int OrderBy { get; set; } = 0;
    }
}