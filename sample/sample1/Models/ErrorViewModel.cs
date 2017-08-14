using System;
using System.ComponentModel.DataAnnotations;

namespace sample1.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    public class TestTable: TestTableBase
    {
        [Key]
        public int kid { get; set; }

       // public string Name { get; set; }
    }

    public class TestTableBase
    {
        public int kbid { get; set; }

        public string BName { get; set; }
    }
}