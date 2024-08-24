using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data;
namespace DatenbankManagementSystem {
    /// <summary>
    /// Database Object that works with the DataSet it gets when it is created.
    /// It also creates Table Objects with the DataTables from the DataSet
    /// </summary>
    public class Database {
        //Original DataSet
        public DataSet dataSet { get; set; }
        //List of Table ojects
        public List<Table> tables { get; set; }
        //Name of the database
        public string databaseName{get;set;}
        //Table of the relationships in the Database between the different tables
        public DataTable tableConstraints { get; set; }
        public Database(DataSet database, DataTable tableConstraints) {
            this.tableConstraints = tableConstraints;
            this.dataSet = database;
            this.databaseName = database.DataSetName;
            if(tables == null) {
                tables = new List<Table>();
            }
            MapTables();
        }
        /// <summary>
        /// Adding new table objects from the dataset after reading the database
        /// </summary>
        private void MapTables() {
            try {
                foreach(DataTable table in dataSet.Tables) {
                    tables.Add(new Table(table,table.TableName));
                }
            }
            catch(Exception) {
                throw;
            }
        }
        /// <summary>
        /// Add table as object and the table to the original dataset
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public Boolean AddTable(DataTable table) {
            tables.Add(new Table(table,table.TableName));
            dataSet.Tables.Add(table);
            return true;
        }
    }
}
