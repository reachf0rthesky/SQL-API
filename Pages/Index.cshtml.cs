using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySqlConnector;
using System.Data;
using System.Text.RegularExpressions;
namespace DatenbankManagementSystem {
    public class IndexModel : PageModel {
        private readonly ILogger<IndexModel> _logger;
        public IndexModel(ILogger<IndexModel> logger) {
            _logger = logger;
        }
        //List of Database, die alle Datenbanken enthält, die zur Laufzeit in das Programm importiert wurden
        [BindProperty] public List<Database>? databases { get; set; }
        //Die Statische Liste der Datenbanken
        private static List<Database>? databasesBase = new List<Database>();
        //Die Art der Aktion, die das Programm ausführt, wenn der Benutzer eine Aktion ausführt
        [BindProperty] public String? action { get; set; }
        //Der ConnectionString zur aktuellen Datenbank
        public String connectionString = new String("");
        //Die Fehlerliste, die verwendet wird, um Fehler für den Benutzer auszugeben, die er später sehen kann
        [BindProperty] public List<Exception>? errorStackTrace { get; set; }
        //Das Schlüsselwort, das der Benutzer zum Suchen oder Filtern je nach Aktion eingibt
        [BindProperty] public String? searchKeyWord { get; set; }
        //Die Art der Suche, die der Benutzer durchführen möchte (Tabellennamen, Spaltennamen oder alle Daten)
        [BindProperty] public String? searchType { get; set; }
        //Die Werte dessen, was bei der Suche gefunden wurde und wo es gefunden wurde
        [BindProperty] public List<String>? searchFoundValues { get; set; }
        //Informationen darüber, was in einer Tabelle geändert werden soll
        [BindProperty] public List<List<String>>? alterInfo { get; set; }
        // Die Spaltennamen der neuen Tabelle
        [BindProperty] public List<String>? tableColNames { get; set; }
        //Der Wertetyp der neuen Tabellenspalten
        [BindProperty] public List<Type>? tableColTypes { get; set; }
        //Zeilen, die der aktuellen Tabelle hinzugefügt werden
        [BindProperty] public List<List<Object>>? tableRowsToAdd { get; set; }
        //Informationen darüber, welche Art von Löschung durchgeführt werden muss
        [BindProperty] public List<String>? deleteInfo { get; set; }
        //Diese 2 Eigenschaften unten ändern sich, je nachdem, welche Tabelle oder Datenbank angezeigt wird, und werden in vielen der Aktionen verwendet
        //Name der aktuell ausgewählten Datenbank
        [BindProperty] public String? databaseName { get; set; }
        //Name der aktuell ausgewählten Tabelle
        [BindProperty] public String? tableName { get; set; }
        //Statische Speicherung des aktuell ausgewählten Datenbanknamens
        private static String? currentDatabaseName;
        //Statische Speicherung des aktuell ausgewählten Tabellennamens
        private static String? currentTableName;
        public void OnPost() {
            try {
                // Regex zum Rausfiltern von Sonderzeichen
                String regex = "[^a-zA-Z0-9]+";
                Regex rgex = new Regex(regex);
                //Neuerstellung der errorStackTrace falls sie null ist
                if(errorStackTrace == null) {
                    errorStackTrace = new List<Exception>();    
                }
                //Datenbanken werden durch statische Datenbanken überschrieben
                databases = databasesBase;
                //Bereits bestehende DBs in das Program laden
                if(action == "addalreadyexistingdbs") {
                    AddAlreadyExistingDbs();
                    databasesBase = databases;
                    return;
                }
                //Falls wir einen aktuellen Db oder Tabellennamen haben übernehmen wir diesen hier
                if(currentDatabaseName != null) {
                    databaseName = currentDatabaseName;
                }
                if(currentTableName != null) {
                    tableName = currentTableName;
                }
                //Hier wird geschaut welches Form Daten enthält und somit vom user benutzt wird und dann werden je nachdem andere Aktionen ausgeführt
                //Datenbank anzeigen
                if(Request.Form["navdatabase"].ToString() != "") {
                    action = Request.Form["navdatabase"].ToString().Split(',')[0];
                    databaseName = Request.Form["navdatabase"].ToString().Split(',')[1];
                    currentDatabaseName = databaseName;
                }
                //Tabelle anzeigen
                if(Request.Form["navdatatable"].ToString() != "") {
                    action = Request.Form["navdatatable"].ToString().Split(',')[0];
                    databaseName = Request.Form["navdatatable"].ToString().Split(',')[1];
                    if(Request.Form["viewTableNames"].ToString() != "") {
                        tableName = Request.Form["viewTableNames"].ToString();
                    }
                    else {
                        tableName = Request.Form["navdatatable"].ToString().Split(',')[2];
                    }
                    //tableName = Request.Form["navdatatable"].ToString().Split(',')[2];
                    currentDatabaseName = databaseName;
                    currentTableName = tableName;
                }
                //Tabellen Relationen erstellen Schritt 1 (Seite zum angeben der nötigen Daten anzeigen)
                if(Request.Form["changeRelationsBtn"].ToString() == "Relationen der Tabellen ändern") {
                    if(GetDatabase().tables.Count < 2) {
                        throw new Exception("Es gibt keine Tabellen in der Datenbank, es werden mindestens 2 benötigt um relationen zu erstellen");
                    }
                    action = Request.Form["changeTableRelations"].ToString();
                    return;
                }
                //Tabellen Relationen erstellen Schritt 2 (Seite zum angeben der nötigen Daten anzeigen)
                if(Request.Form["changeRelations"].ToString() == "Primär- und Fremdschlüssel Verbindung hinzufügen") {
                    alterInfo = new List<List<string>>();
                    //Liste von Infos die zu alterInfo hinzugefügt werden und später verarbeitet werden
                    List<String> relationTargets = new List<string>();
                    if(Request.Form["primaryKey"].ToString().Split(',')[1].ToLower() != Request.Form["foreignKey"].ToString().Split(',')[1].ToLower()) {
                        throw new Exception("Der Primary und Foreign Key müssen den selben Namen haben");
                    }
                    if(Request.Form["primaryKey"].ToString() == Request.Form["foreignKey"].ToString()) {
                        throw new Exception("Der Primary und Foreign Key können nicht derselbe sein");
                    }
                    action = Request.Form["changeTableRelations"].ToString().Split(',')[0];
                    databaseName = Request.Form["changeTableRelations"].ToString().Split(',')[1];
                    tableName = Request.Form["changeTableRelations"].ToString().Split(',')[2];
                    relationTargets.Add("relations");
                    relationTargets.Add(Request.Form["primaryKey"].ToString().Split(',')[0]);
                    relationTargets.Add(Request.Form["primaryKey"].ToString().Split(',')[1]);
                    relationTargets.Add(Request.Form["foreignKey"].ToString().Split(',')[0]);
                    relationTargets.Add(Request.Form["foreignKey"].ToString().Split(',')[1]);
                    alterInfo.Add(relationTargets);
                }
                //Datenbank erstellen Schritt 1 (Seite zum angeben der nötigen Daten anzeigen)
                if(Request.Form["createDatabase"].ToString() != "") {
                    action = Request.Form["createDatabase"].ToString();
                    return;
                }
                //Datenbank erstellen Schritt 2
                if(Request.Form["txtNewDataBase"].ToString() != "") {
                    if(rgex.IsMatch(Request.Form["txtNewDataBase"].ToString())) {
                        throw new Exception("Der Datenbankname darf keine Sonderzeichen enthalten :"+ Request.Form["txtNewDataBase"].ToString());
                    }
                    action = "createDb";
                    databaseName = Request.Form["txtNewDataBase"].ToString();
                }
                //Neue Tabelle erstellen Schritt 1 (Seite zum angeben der nötigen Daten anzeigen)
                if(Request.Form["createBtn"].ToString() == "Neue Tabelle erstellen") {
                    action = Request.Form["createnewTable"].ToString().Split(',')[0];
                    databaseName = Request.Form["createnewTable"].ToString().Split(',')[1];
                    currentDatabaseName = databaseName;
                    return;
                }
                //Neue Tabelle erstellen Schritt 2
                if(Request.Form["txtNewDataTable"].ToString() != "") {
                    if(rgex.IsMatch(Request.Form["txtNewDataTable"].ToString())) {
                        throw new Exception("Der Tabellenname darf keine Sonderzeichen enthalten: "+Request.Form["txtNewDataTable"].ToString());
                    }
                    tableColNames = new List<string>();
                    tableColTypes = new List<Type>();
                    action = "addtable";
                    tableName = Request.Form["txtNewDataTable"].ToString();
                    //Prüfen welche Spalten ausgefüllt sind. Mindestens 1 ist erforderlich.
                    for(int i = 1; i <= 5; i++) {
                        if(Request.Form["columnName" + i].ToString() != "") {
                            if(rgex.IsMatch(Request.Form["columnName" + i].ToString())) {
                                throw new Exception("Der Spaltenname darf keine Sonderzeichen enthalten: "+ Request.Form["columnName" + i].ToString());
                            }
                            tableColNames.Add(Request.Form["columnName" + i]);
                            if(Request.Form["columnType" + i] == "varchar") {
                                tableColTypes.Add("".GetType());
                            }
                            else if(Request.Form["columnType" + i] == "integer") {
                                tableColTypes.Add(0.GetType());
                            }
                            else if(Request.Form["columnType" + i] == "decimal") {
                                tableColTypes.Add(0.0.GetType());
                            }
                        }
                    }
                }
                //Tabelle ändern Schritt 1 (Seite zum angeben der nötigen Daten anzeigen)
                if(Request.Form["alterBtn"].ToString() != "") {
                    action = Request.Form["alterTable"].ToString().Split(',')[0];
                    databaseName = Request.Form["alterTable"].ToString().Split(',')[1];
                    tableName = Request.Form["alterTable"].ToString().Split(',')[2];
                    currentDatabaseName = databaseName;
                    currentTableName = tableName;
                    return;
                }
                //Tabelle ändern Schritt 2
                if(Request.Form["AlterTable"].ToString() != "") {
                    alterInfo = new List<List<string>>();
                    databaseName = Request.Form["alterTable"].ToString().Split(',')[1];
                    action = Request.Form["alterTable"].ToString().Split(',')[0];
                    tableName = Request.Form["alterTable"].ToString().Split(',')[2];
                    if(Request.Form["altertableaddcol"].ToString().ToLower() == "submit") {
                        if(Request.Form["newcolname"].ToString() != "") {
                            if(Request.Form["newcoltype"].ToString() != "") {
                                List<String> tempList = new List<string>();
                                tempList.Add("add");
                                tempList.Add(Request.Form["newcolname"].ToString());
                                tempList.Add(Request.Form["newcoltype"].ToString());
                                alterInfo.Add(tempList);
                            }
                            else {
                                throw new Exception("Sie müssen einen Tabellendatentyp eingeben wenn sie eine neue Spalte erstellen wollen");
                            }
                        }
                        else {
                            throw new Exception("Sie müssen einen Tabellennamen eingeben wenn sie eine neue Spalte erstellen wollen");
                        }
                    }
                    foreach(DataColumn col in GetTable().table.Columns) {
                        //Falls Datentyp geändert werden soll
                        if(Request.Form[col.ColumnName + ",Datentyp"].ToString() != "") {
                            List<String> tempList = new List<string>();
                            tempList.Add("modify");
                            tempList.Add(col.DataType.Name.ToString());
                            tempList.Add(Request.Form[col.ColumnName + ",Datentyp"].ToString());
                            tempList.Add(col.ColumnName.ToString());
                            alterInfo.Add(tempList);
                        }
                        //Falls name geändert werden soll
                        if(Request.Form[col.ColumnName + ",Name"].ToString() != "") {
                            List<String> tempList = new List<string>();
                            tempList.Add("rename");
                            tempList.Add(col.ColumnName.ToString());
                            tempList.Add(Request.Form[col.ColumnName + ",Name"].ToString());
                            tempList.Add(Request.Form[col.ColumnName + ",Datentyp"].ToString());
                            alterInfo.Add(tempList);
                        }
                    }
                }
                //Delete Table or Database
                if(Request.Form["deleteBtn"].ToString() != "") {
                    deleteInfo = new List<string>();
                    action = Request.Form["deleteDatabase"].ToString().Split(',')[0];
                    deleteInfo.Add(Request.Form["deleteDatabase"].ToString().Split(',')[1]);
                    if(currentDatabaseName != null) {
                        databaseName = currentDatabaseName;
                    }
                    if(currentTableName != null) {
                        tableName = currentTableName;
                    }
                    connectionString = $"server = localhost; user = root; password =; database ={databaseName}; ";
                }
                //Filter Databases
                if(Request.Form["searchBtn"].ToString() != "") {
                    if(Request.Form["searchkeyword"].ToString() == "") {
                        throw new Exception("Bitte etwas beim Suchtext eingeben");
                    }
                    action = Request.Form["searchDatabases"].ToString();
                    searchKeyWord = Request.Form["searchkeyword"].ToString();
                    searchType = Request.Form["searchType"].ToString();
                }
                //Add table entry step 1
                if(Request.Form["createBtn"].ToString() == "Neue Reihe hinzufügen") {
                    action = Request.Form["createnewtablerow"].ToString().Split(',')[0];
                    databaseName = Request.Form["createnewtablerow"].ToString().Split(',')[1];
                    tableName = Request.Form["createnewtablerow"].ToString().Split(',')[2];
                    currentTableName = tableName;
                    currentDatabaseName = databaseName;
                }
                //Add table entry step 2
                if(Request.Form["createBtn"].ToString() == "Tabellenreihe erstellen") {
                    int i = 1;
                    if(currentTableName != null) {
                        tableName = currentTableName;
                    }
                    if(currentDatabaseName != null) {
                        databaseName = currentDatabaseName;
                    }
                    List<object> rowAttributes = new List<object>();
                    tableRowsToAdd = new List<List<object>>();
                    action = "addtableentries";
                    foreach(DataColumn col in GetTable().table.Columns) {
                        if(ParseString(Request.Form["columnname" + i]).GetType() == col.DataType || col.DataType=="".GetType()) {
                            rowAttributes.Add(Request.Form["columnname" + i]);
                        }
                        else {
                            throw new Exception("Einer der Datentypen beim Tabellen hinzufügen hat nicht gepasst: " + col.ColumnName);
                        }
                        i++;
                    }
                    tableRowsToAdd.Add(rowAttributes);
                }
                // Erstellen Sie eine neue Datenbank und fügen Sie sie als Objekt zum Programm hinzu
                if(action.ToLower() == "createdb") {
                    if(DatabaseExistsCheck()) {
                        connectionString = $"server = localhost; user = root; password =; database ={databaseName}; ";
                        CreateNewDatabaseFile();
                        AddDatabase();
                        action = "viewdatabase";
                    }
                    else {
                        return;
                    }
                }
                // Hinzufügen einer Tabelle zur bestehenden Datenbank
                else if(action.ToLower() == "addtable" && databases.Count >= 1) {
                    if(currentDatabaseName != null) {
                        databaseName = currentDatabaseName;
                    }
                    connectionString = $"server = localhost; user = root; password =; database ={databaseName}; ";
                    AddTable();
                    action = "viewdatatable";
                }
                // Hinzufügen eines Tabelleneintrages zu einer bestehenden Tabelle
                else if(action.ToLower() == "addtableentries" && databases.Count >= 1) {
                    AddTableEntrys();
                    action = "viewdatatable";
                }
                // Durchsuchen einer vorhandenen Datenbank nach Schlüsselwörtern
                else if(action.ToLower() == "searchkeyword" && databases.Count >= 1 && searchKeyWord != null) {
                    SearchKeyWordInDB();
                    action = "showFilterErg";
                }
                //Löschen einer Tabelle oder Datenbank auf der Grundlage von deleteInfo
                else if(action.ToLower() == "delete" && databases.Count >= 1) {
                    DeleteTableOrDatabase();
                    action = "startsite";
                }
                //Tabellenattribute ändern
                else if(action.ToLower() == "altertable" && databases.Count > 0 && GetDatabase().tables.Count > 0) {
                    AlterTable();
                    action = "viewdatatable";
                    if(currentDatabaseName != null) {
                        databaseName = currentDatabaseName;
                    }
                    if(currentTableName != null) {
                        tableName = currentTableName;
                    }
                }
                databasesBase = databases;
            }
            catch(Exception ex) {
                if(!errorStackTrace.Contains(ex)) {
                    errorStackTrace.Add(ex);
                }
                return;
            }
        }
        //
        // Methoden Start
        //
        //Überprüft, welcher Datentyp eine Zeichenkette ist und gibt die Zeichenkette als diesen Typ zurück
        private object ParseString(string str) {
            Int32 intValue;
            Int64 bigintValue;
            double doubleValue;
            if(Int32.TryParse(str, out intValue))
                return intValue;
            else if(Int64.TryParse(str, out bigintValue))
                return bigintValue;
            else if(double.TryParse(str, out doubleValue))
                return doubleValue;
            else return str;
        }
        /// <summary>
        /// Schaut ob es bereits Datenbanken in Mysql gibt und läd diese in das Program
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        public Boolean AddAlreadyExistingDbs() {
            try {
                databases = databasesBase;
                this.connectionString = "SERVER=localhost;UID='root';" + "PASSWORD=;";
                using(MySqlConnection connection = new MySqlConnection(connectionString)) {
                    MySqlDataAdapter adapter = new MySqlDataAdapter("SHOW DATABASES;", connection);
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    String[] systemDbNames = { "information_schema", "mysql", "performance_schema", "phpmyadmin" };
                    foreach(DataRow row in table.Rows) {
                        if(!systemDbNames.Contains(row[table.Columns[0]].ToString())) {
                            this.connectionString = $"server = localhost; user = root; password =; database ={row[table.Columns[0]]}; ";
                            databaseName = row[table.Columns[0]].ToString();
                            Boolean dbExists = false;
                            foreach(Database db in databases) {
                                if(databaseName == db.databaseName) {
                                    dbExists = true;
                                }
                            }
                            if(dbExists) {
                                continue;
                            }
                            AddDatabase();
                        }
                    }
                }
                return true;
            }
            catch(Exception ex) {
                errorStackTrace.Add(ex);
                throw;
            }
        }
        /// <summary>
        /// Schaut ob die Datenbank bereits existiert mit einer Schleife und einem Vergleich
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        private Boolean DatabaseExistsCheck() {
            try {
                foreach(Database database in databases) {
                    if(database.databaseName == databaseName) {
                        throw new Exception("Database with the same name is already loaded in the Program");
                    }
                }
                return true;
            }
            catch(Exception ex) {
                errorStackTrace.Add(ex);
                return false;
            }
        }
        /// <summary>
        /// Erstellung einer neuen, leeren MySQL-Datenbank, wenn dies erforderlich ist
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        private Boolean CreateNewDatabaseFile() {
            try {
                string connectionString = $"server=localhost;uid=root;pwd=;";
                string databaseName = this.databaseName;
                using(MySqlConnection connection = new MySqlConnection(connectionString)) {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand($"CREATE DATABASE {databaseName};", connection);
                    command.ExecuteNonQuery();
                    this.connectionString = connectionString + "database=" + databaseName + ";";
                }
                return true;
            }
            catch(Exception ex) {
                errorStackTrace.Add(ex);
                return false;
            }
        }
        /// <summary>
        /// Füllt alle Tabellen einer Datenbank in ein Dataset, um später damit zu arbeiten
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        private Boolean AddDatabase() {
            try {
                DataSet database = new DataSet();
                DataTable tableConstraints = new DataTable();
                DataTable tableNames = new DataTable();
                using(MySqlConnection connectionDB = new MySqlConnection(connectionString)) {
                    connectionDB.Open();
                    //Datenbankname
                    database.DataSetName = connectionDB.Database;
                    //Tabellennamen mit Sql-Abfrage abrufen
                    MySqlCommand command = new MySqlCommand("Show tables;", connectionDB);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                    adapter.Fill(tableNames);
                    if(tableNames == null) {
                        throw new Exception("There was an issue with the tablenames when importing the " + connectionDB.Database + " Database");
                    }
                    //Abrufen von Beziehungen aus Tabellen in der Datenbank mit Sql-Befehl (Quelle ist im Dokument)
                    command = new MySqlCommand("SELECT\n\r" +
                                                "  `TABLE_NAME`,                            -- Foreign key table\n\r" +
                                                "  `COLUMN_NAME`,                           -- Foreign key column\n\r" +
                                                "  `REFERENCED_TABLE_NAME`,                 -- Origin key table\n\r" +
                                                "  `REFERENCED_COLUMN_NAME`                 -- Origin key column\n\r" +
                                                "FROM\n\r" +
                                                "  `INFORMATION_SCHEMA`.`KEY_COLUMN_USAGE`  -- Will fail if user don't have privilege\n\r" +
                                                "WHERE\n\r" +
                                                $"  `CONSTRAINT_SCHEMA` ='{connectionDB.Database}'\n\r" 
                                                , connectionDB);
                    adapter = new MySqlDataAdapter(command);
                    adapter.Fill(tableConstraints);
                    // Holt jede Tabelle mit Select * aus der Datenbank und speichern Sie sie alle in einem Dataset.
                    foreach(DataRow rowZero in tableNames.Rows) {
                        String tableName = rowZero.ItemArray[0].ToString();
                        string query = "SELECT * FROM " + tableName.ToString();
                        command = new MySqlCommand(query, connectionDB);
                        adapter.SelectCommand = command;
                        DataTable dataTable = new DataTable(tableName.ToString());
                        adapter.Fill(dataTable);
                        database.Tables.Add(dataTable);
                    }
                    // Datenbankobjekt wird mit dem Datensatz erstellt und zur Liste der Datenbanken hinzugefügt
                    Database db = new Database(database, tableConstraints);
                    databases.Add(db);
                }
                //Bei Erfolg wird true zurückgegeben
                return true;
            }
            catch(Exception ex) {
                //Bei Fehler wird false zurückgegeben und die Ausnahme zum ErrorStackTrace hinzugefügt
                errorStackTrace.Add(ex);
                return false;
            }
        }
        /// <summary>
        /// Sucht nach Schlüsselwörtern in verschiedenen Stilen, je nachdem, was der Benutzer suchen möchte:
        /// Tabellennamen, Spaltennamen oder Datenzellen
        /// Wenn der Suchtyp Daten ist, werden auch die Tabellen- und Spaltennamen durchsucht.
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        private Boolean SearchKeyWordInDB() {
            try {
                Boolean erg = false;
                int rowPosition = 1;
                foreach(Database database in databases) {
                    if(database.databaseName == databaseName) {
                        foreach(Table table in database.tables) {
                            if(searchType == "table" || searchType == "data") {
                                if(table.table.TableName.ToLower().Contains(searchKeyWord.ToLower())) {
                                    erg = true;
                                    searchFoundValues.Add(database.databaseName + " " + table.table.TableName);
                                }
                            }
                            if(searchType == "column" || searchType == "data") {
                                foreach(DataColumn column in table.table.Columns) {
                                    if(column.ColumnName.ToLower().Contains(searchKeyWord.ToLower())) {
                                        erg = true;
                                        searchFoundValues.Add(database.databaseName + " " + table.table.TableName + " " + column.ColumnName);
                                    }
                                }
                            }
                            if(searchType == "data") {
                                foreach(DataRow row in table.table.Rows) {
                                    foreach(DataColumn column in table.table.Columns) {
                                        if(row[column].ToString().ToLower().Contains(searchKeyWord.ToLower())) {
                                            erg = true;
                                            searchFoundValues.Add(database.databaseName + " " + table.table.TableName + " " + column.ColumnName + " " + "Reihe:" + rowPosition);
                                        }
                                    }
                                    rowPosition++;
                                }
                            }
                        }
                    }
                }
                return erg;
            }
            catch(Exception ex) {
                errorStackTrace.Add(ex);
                return false;
            }
        }
        /// <summary>
        /// Fügt eine Tabelle zu einer bestehenden Datenbank hinzu
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        private Boolean AddTable() {
            Boolean erg = false;
            try {
                foreach(Database database in databases) {
                    if(database.databaseName == databaseName) {
                        //Sobald die passende Db gefunden wurde, fügen wir eine Tabelle hinzu
                        DataTable table = new DataTable(tableName);
                        for(int i = 0; i < tableColNames.Count; i++) {
                            table.Columns.Add(tableColNames[i], tableColTypes[i]);
                        }
                        database.AddTable(table);
                        database.dataSet.AcceptChanges();
                        erg = true;
                        using(MySqlConnection connectionDB = new MySqlConnection(connectionString)) {
                            connectionDB.Open();
                            String query = $"CREATE TABLE {tableName} (";
                            for(int i = 0; i < tableColNames.Count; i++) {
                                query += $"`{tableColNames[i]}` {tableColTypes[i].Name},";
                            }
                            query = query.TrimEnd(',');
                            query += ");";
                            query = query.Replace("String", "varChar(25)").Replace("Int32", "Int").Replace("Double", "Decimal");
                            MySqlCommand command = new MySqlCommand(query, connectionDB);
                            command.ExecuteNonQuery();
                        }
                    }
                }
                return erg;
            }
            catch(Exception ex) {
                errorStackTrace.Add(ex);
                return false;
            }
        }
        /// <summary>
        /// Tabelleneinträge, die sich in der tableRowsToAdd befinden, werden der aktuell verwendeten Tabelle hinzugefügt
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        private Boolean AddTableEntrys() {
            int counter = 0;
            try {
                this.connectionString = $"server = localhost; user = root; password =; database ={databaseName}; ";
                Table table = GetTable();
                foreach(List<Object> tableRow in tableRowsToAdd) {
                    DataRow row = table.table.NewRow();
                    foreach(DataColumn col in table.table.Columns) {
                        row[col] = ParseString(tableRow[counter].ToString());
                        counter++;
                    }
                    table.table.Rows.Add(row);
                    counter = 0;
                    using(MySqlConnection connectionDB = new MySqlConnection(connectionString)) {
                        connectionDB.Open();
                        String query = $"INSERT INTO {tableName} (";
                        for(int i = 0; i < table.table.Columns.Count; i++) {
                            query += $"`{table.table.Columns[i].ColumnName}`,";
                        }
                        query = query.TrimEnd(',');
                        query += ")";
                        query += " VALUES (";
                        for(int i = 0; i < table.table.Columns.Count; i++) {
                            if(table.table.Rows[table.table.Rows.Count - 1].ItemArray[i]?.GetType() == typeof(String)) {
                                query += $"'{table.table.Rows[table.table.Rows.Count - 1].ItemArray[i]}',";
                            }
                            else {
                                query += $"{table.table.Rows[table.table.Rows.Count - 1].ItemArray[i]},";
                            }
                        }
                        query = query.TrimEnd(',');
                        query += ");";
                        MySqlCommand command = new MySqlCommand(query, connectionDB);
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch(Exception ex) {
                errorStackTrace.Add(ex);
                return false;
            }
        }
        /// <summary>
        /// Ruft das Tabellenobjekt basierend auf der aktuell verwendeten Datenbank und der aktuell verwendeten Tabelle ab
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        public Table GetTable() {
            try {
                foreach(Database database in databases) {
                    if(database.databaseName == databaseName) {
                        //Sobald die passende Db gefunden wurde, beginnen wir mit der Suche in der Tabelle
                        foreach(Table table in database.tables) {
                            if(table.tableName == tableName) {
                                //Sobald die passende Tabelle gefunden wurde, wird sie zurückgegeben
                                return table;
                            }
                        }
                    }
                }
                throw new Exception("Die Tabelle wurde nicht gefunden in GetTable()");
            }
            catch(Exception ex) {
                errorStackTrace.Add(ex);
                throw;
            }
        }
        /// <summary>
        /// Ruft das Datenbankobjekt basierend auf der aktuell verwendeten Datenbank ab
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        public Database GetDatabase() {
            try {
                foreach(Database database in databases) {
                    if(database.databaseName == databaseName) {
                        //Wenn die passende Db gefunden wurde, fügen wir eine Tabelle hinzu
                        return database;
                    }
                }
                throw new Exception("Die Datenbank wurde nicht gefunden in GetDatabase()");
            }
            catch(Exception ex) {
                errorStackTrace.Add(ex);
                throw;
            }
        }
        /// <summary>
        /// Die ausgewählte Tabelle oder Datenbank, die der Benutzer zum Löschen ausgewählt hat, wird im Datenbank- und Tabellenobjekt gelöscht.
        /// Sie wird auch im Dataset-Objekt gelöscht, oder im Fall von Datenbanken wird der gesamte Dataset gelöscht.
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        private Boolean DeleteTableOrDatabase() {
            try {
                using(MySqlConnection connection = new MySqlConnection(connectionString)) {
                    connection.Open();
                    if(deleteInfo[0].ToLower() == "table") {
                        Database database = GetDatabase();
                        Table table = GetTable();
                        if(table != null) {
                            database.dataSet.Tables.Remove(table.table);
                            database.tables.Remove(table);
                            database.dataSet.AcceptChanges();
                            MySqlCommand command = new MySqlCommand($"DROP TABLE {tableName};", connection);
                            command.ExecuteNonQuery();
                        }
                        else {
                            throw new Exception($"Die Tabelle {tableName} von der Datenbank {databaseName} konnte nicht gelöscht werden, da sie nicht gefunden wurde.");
                        }
                    }
                    else if(deleteInfo[0].ToLower() == "database") {
                        Database database = GetDatabase();
                        if(database != null) {
                            databases?.Remove(database);
                            database.dataSet.Clear();
                            database.dataSet = null;
                            database = null;
                            MySqlCommand command = new MySqlCommand($"DROP DATABASE {databaseName};", connection);
                            command.ExecuteNonQuery();
                        }
                        else {
                            throw new Exception($"Die Datenbank {databaseName} konnte nicht gelöscht werden, da sie nicht gefunden wurde.");
                        }
                    }
                }
                return true;
            }
            catch(Exception ex) {
                errorStackTrace.Add(ex);
                return false;
            }
        }
        /// <summary>
        /// Die Tabelle wird entsprechend der Auswahl des Benutzers geändert.
        /// Merkmale: Ändern von Spaltennamen und Attributen und Ändern der Beziehungen zwischen Tabellen
        /// </summary>
        /// <returns>True, wenn erfolgreich, false, wenn erfolglos</returns>
        private Boolean AlterTable() {
            try {
                connectionString = $"server = localhost; user = root; password =; database ={databaseName}; ";
                using(MySqlConnection connection = new MySqlConnection(connectionString)) {
                    connection.Open();
                    //Columnname ändern
                    foreach(List<String> list in alterInfo) {
                        String query = $"ALTER TABLE {tableName} ";
                        if(list[0] == "rename") {
                            query += $"CHANGE `{list[1]}` `{list[2]}` {list[3]};";
                            query = query.Replace("String", "varChar(25)").Replace("Int32", "Int").Replace("Double", "Decimal");
                        }
                        else if(list[0] == "relations") {
                            query = $"ALTER TABLE {list[1]} ";
                            query += $"ADD CONSTRAINT `PK_{list[1]}` PRIMARY KEY({ list[2]}); ";
                            query += $"ALTER TABLE {list[3]} ";
                            query += $"ADD CONSTRAINT `FK_{list[3]}` FOREIGN KEY({list[4]}) REFERENCES `{list[1]}`({list[2]}); ";
                        }
                        //Columntyp ändern
                        else if(list[0] == "modify") {
                            query += $"CHANGE COLUMN `{list[3]}` `{list[3]}` {list[2]};";
                            query = query.Replace("String", "varChar(25)").Replace("Int32", "Int").Replace("Double", "Decimal");
                        }
                        //Column hinzufügen
                        else if(list[0] == "add") {
                            query += $"ADD `{list[1]}` {list[2]};";
                            query = query.Replace("String", "varChar(25)").Replace("Int32", "Int").Replace("Double", "Decimal");
                        }
                        MySqlCommand command = new MySqlCommand(query, connection);
                        command.ExecuteNonQuery();
                        Thread.Sleep(5);
                    }
                }
                databases.Clear();
                databasesBase.Clear();
                AddAlreadyExistingDbs();
                return true;
            }
            catch(Exception ex) {
                errorStackTrace.Add(ex);
                throw;
            }
        }
        //
        // Methods End
        //
    }
}