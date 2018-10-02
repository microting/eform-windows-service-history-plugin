﻿namespace HistorySql
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class fields
    {
        public fields()
        {
        }

        [Key]
        public int id { get; set; }

        [StringLength(255)]
        public string workflow_state { get; set; }

        public int? version { get; set; }

        public DateTime? created_at { get; set; }

        public DateTime? updated_at { get; set; }

        [ForeignKey("check_list")]
        public int? check_list_id { get; set; }

        public int? sdk_field_id { get; set; }

        public virtual check_lists check_list { get; set; }

    }
}
