using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utility.DataModels
{
    public class TestData
    {
        public string? DataSetName { get; set; }
        public string? State { get; set; }
        //Stores all the test data for test cases every entry has one test case [0] FieldName, [1] InputVlaue, [2] ExpectedValue, [3] XPath
        public List<string[]> Data { get; set; } 
    }
}
