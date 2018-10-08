namespace HistorySql
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class check_lists
    {
        public check_lists()
        {   
            this.fields = new HashSet<fields>();
        }

        [Key]
        public int id { get; set; }

        [StringLength(255)]
        public string workflow_state { get; set; }

        public int? version { get; set; }

        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        public int? sdk_check_list_id { get; set; }

        public int? reset_values_field_id { get; set; } // If this field is filled, skip field_values for other fields.

        public virtual ICollection<fields> fields { get; set; }
    }
}
