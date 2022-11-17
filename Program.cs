/// Автор: Клинков Никита Сергеевич
/// Задача: Сделать nse скрипт для nmap (https://nmap.org), по аналогии с существующими
/// который будет определять версию Apache Tomcat по странице 404
/// Кандидат должен сам развернуть свой сервер с Apache Tomcat на виртуалке и быть готовым предоставить нам доступ.
///
/// Далее необходимо написать консольное приложение на .Net 6, в котором необходимо вызвать запуск nmap с этим скриптом,
/// получить результат, оттуда вытащить версию Apache Tomcat и положить в БД (PostgreSQL) в произвольную таблицу.
///
/// Результат необходимо залить в GitHub или подобный ему сервис и предоставить нам доступ.
/// Консольная программа тестировалась в связке с:
///     - docker-контейнер kwnikita/apache_tomcat_void c apache_tomcat v. 10.1.1
///     - nmap v. 7.80
///     - postgresql v. 15.1
///     
/// Скрипт для nmap (my_script_1.nse) нужно положить в папку .../Nmap/scipts/ 
/// 
/// К коду так же добавлен пакет NuGet:
///         -npgsql - пакет предаставляющий решения для взаимодействия с СУБД PostgreSQL


using Npgsql;
using System.Diagnostics;
using System.Text.RegularExpressions;
internal static class Program
{
    private static void Main(string[] args)
    {
        string host_WebServer = "127.0.0.1";
        string post_WebServer = "8888";
        string host_db = host_WebServer;
        string port_db = "5432";
        string name_host = "postgres";
        string password = "postgres";
        string name_db = "postgres";

        //Настраиваем команду для вызова подготовленного скрипта
        ProcessStartInfo psi = new ProcessStartInfo("nmap",
            $"--script=my_script_1.nse -n {host_WebServer} -p {post_WebServer} --unprivileged -Pn");
        psi.UseShellExecute = false;
        psi.RedirectStandardOutput = true;

        //Создаём команду и присваиваем настройки psi
        Process pProcess = new Process();
        pProcess.StartInfo = psi;
        string strOutput;

        //Запускаем команду
        try
        {
            pProcess.Start();
            //Записываем результат
            strOutput = pProcess.StandardOutput.ReadToEnd();
            pProcess.WaitForExit();
            pProcess.Close();

        }catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }

        //Берём из результат выполненния информацию о версии Apache Tomcat
        Regex regex = new Regex(@"my_script_1: (\S*)");
        MatchCollection matches = regex.Matches(strOutput);
        string result = matches[0].Groups[1].ToString();

        //Полученный результат отправляем на базу данных в таблицу ApacheVer_log
        string connString = $"Host={host_db}:{port_db};Username={name_host};Password={password};Database={name_db}";
        NpgsqlConnection connectDB = new NpgsqlConnection(connString);
        try
        {
            connectDB.Open();
            //Создаём таблицу, если её нету, а потом добавляем данные о версии
            NpgsqlCommand command = new NpgsqlCommand(
            "CREATE TABLE IF NOT EXISTS apache_ver_log(" +
            "id            UUID DEFAULT gen_random_uuid()," +
            "Version       VARCHAR(40)," +
            "date_check    TIMESTAMP DEFAULT NOW());" +
            $"INSERT INTO apache_ver_log (Version) VALUES('{result}')"
            , connectDB);

            command.ExecuteNonQuery();
            connectDB.Close();
        }catch(Exception ex) 
        {
            connectDB.Close();
            Console.WriteLine(ex.Message);
            return;
        }
        Console.WriteLine("End");
    }
}
