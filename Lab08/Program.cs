using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using ScottPlot;

namespace Lab08
{
    class Program
    {
        static void Main()
        {
            int n = 5; // количество каналов
            double mu = 2.0; // интенсивность обслуживания 
            int totalRequests = 50; // количество заявок для одного эксперимента
            List<double> xs = new List<double>();

            List<double> p0_theor_list = new List<double>(), p0_exp_list = new List<double>();
            List<double> pref_theor_list = new List<double>(), pref_exp_list = new List<double>();
            List<double> q_theor_list = new List<double>(), q_exp_list = new List<double>();
            List<double> a_theor_list = new List<double>(), a_exp_list = new List<double>();
            List<double> k_theor_list = new List<double>(), k_exp_list = new List<double>();

            List<int> reqCount_list = new List<int>();
            List<int> procCount_list = new List<int>();
            List<int> rejCount_list = new List<int>();

            for (double lambda = 2.0; lambda <= 20.0; lambda += 2.0)
            {
                int delayBetweenRequestsMs = (int)(1000.0 / lambda);

                Server server = new Server();
                Client client = new Client(server);

                for (int id = 1; id <= totalRequests; id++)
                {
                    client.Send(id);
                    Thread.Sleep(delayBetweenRequestsMs);
                }

                Thread.Sleep(800); 
                double rho = lambda / mu;
                double sum = 0;
                for (int i = 0; i <= n; i++) sum += Math.Pow(rho, i) / Factorial(i);

                double p0_theor = 1.0 / sum;
                double pref_theor = (Math.Pow(rho, n) / Factorial(n)) * p0_theor;
                double q_theor = 1.0 - pref_theor;
                double a_theor = lambda * q_theor;
                double k_theor = a_theor / mu;

                double q_exp = (double)server.processedCount / server.requestCount;
                double pref_exp = (double)server.rejectedCount / server.requestCount;
                double a_exp = lambda * q_exp; 
                double k_exp = a_exp / mu;

                double expSum = 0;
                for (int i = 0; i <= n; i++) expSum += Math.Pow(k_exp / q_exp, i) / Factorial(i);
                double p0_exp = 1.0 / expSum;
                xs.Add(lambda);
                p0_theor_list.Add(p0_theor); p0_exp_list.Add(p0_exp);
                pref_theor_list.Add(pref_theor); pref_exp_list.Add(pref_exp);
                q_theor_list.Add(q_theor); q_exp_list.Add(q_exp);
                a_theor_list.Add(a_theor); a_exp_list.Add(a_exp);
                k_theor_list.Add(k_theor); k_exp_list.Add(k_exp);
                reqCount_list.Add(server.requestCount);
                procCount_list.Add(server.processedCount);
                rejCount_list.Add(server.rejectedCount);
                
                Console.WriteLine($"\n--- Итерация для λ = {lambda} ---");
                Console.WriteLine("Всего заявок: {0}", server.requestCount);
                Console.WriteLine("Обработано заявок: {0}", server.processedCount);
                Console.WriteLine("Отклонено заявок: {0}", server.rejectedCount);
            }

            using (StreamWriter sw = new StreamWriter("../result/results.txt", false, Encoding.UTF8))
            {
                sw.WriteLine("ОТЧЕТ ПО РЕЗУЛЬТАТАМ МОДЕЛИРОВАНИЯ СМО «КЛИЕНТ-СЕРВЕР»");
                sw.WriteLine($"Параметры системы:");
                sw.WriteLine($"- Количество каналов (n): {n}");
                sw.WriteLine($"- Интенсивность обслуживания (μ): {mu} заявок/сек");
                sw.WriteLine($"- Количество заявок в одной итерации: {totalRequests}");
                sw.WriteLine("\nТаблица результатов (Теория / Эксперимент):");
                sw.WriteLine("Лямбда\tP0_теор\tP0_эксп\tPref_теор\tPref_эксп\tQ_теор\tQ_эксп\tA_теор\tA_эксп\tK_теор\tK_эксп");

                for (int i = 0; i < xs.Count; i++)
                {
                    sw.WriteLine($"{xs[i]}\t{p0_theor_list[i]:F4}\t{p0_exp_list[i]:F4}\t" +
                                 $"{pref_theor_list[i]:F4}\t{pref_exp_list[i]:F4}\t" +
                                 $"{q_theor_list[i]:F4}\t{q_exp_list[i]:F4}\t" +
                                 $"{a_theor_list[i]:F4}\t{a_exp_list[i]:F4}\t" +
                                 $"{k_theor_list[i]:F4}\t{k_exp_list[i]:F4}");
                }
            }
            double[] xArr = xs.ToArray();

            GeneratePlot("Вероятность простоя системы (P0)", xArr, p0_theor_list.ToArray(), p0_exp_list.ToArray(), "../result/p-1.png");
            GeneratePlot("Вероятность отказа системы (Pref)", xArr, pref_theor_list.ToArray(), pref_exp_list.ToArray(), "../result/p-2.png");
            GeneratePlot("Относительная пропускная способность (Q)", xArr, q_theor_list.ToArray(), q_exp_list.ToArray(), "../result/p-3.png");
            GeneratePlot("Абсолютная пропускная способность (A)", xArr, a_theor_list.ToArray(), a_exp_list.ToArray(), "../result/p-4.png");
            GeneratePlot("Среднее число занятых каналов (K)", xArr, k_theor_list.ToArray(), k_exp_list.ToArray(), "../result/p-5.png");
        }

        static void GeneratePlot(string title, double[] x, double[] yTheor, double[] yExp, string filepath)
        {
            var plt = new ScottPlot.Plot();
            var s1 = plt.Add.Scatter(x, yTheor);
            s1.LegendText = "Теоретическое значение";
            s1.LineWidth = 2;
            var s2 = plt.Add.Scatter(x, yExp);
            s2.LegendText = "Экспериментальное значение";
            s2.LineWidth = 2;
            s2.LinePattern = LinePattern.Dashed;
            plt.Title(title);
            plt.XLabel("Интенсивность входного потока (λ)");
            plt.ShowLegend();
            plt.SavePng(filepath, 800, 600);
        }

        static double Factorial(int num)
        {
            if (num <= 1) return 1;
            return num * Factorial(num - 1);
        }
    }

    struct PoolRecord
    {
        public Thread thread;
        public bool in_use;
    }

    class Server
    {
        private PoolRecord[] pool;
        private object threadLock = new object();
        public int requestCount = 0;
        public int processedCount = 0;
        public int rejectedCount = 0;

        public Server()
        {
            pool = new PoolRecord[5];
        }

        public void Proc(object sender, ProcEventArgs e)
        {
            lock (threadLock)
            {
                requestCount++;
                for (int i = 0; i < 5; i++)
                {
                    if (!pool[i].in_use)
                    {
                        pool[i].in_use = true;
                        pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                        pool[i].thread.Start(new Tuple<int, int>(i, e.id));
                        processedCount++;
                        return;
                    }
                }
                rejectedCount++;
            }
        }

        public void Answer(object arg)
        {
            var data = (Tuple<int, int>)arg;
            int poolIndex = data.Item1;
            Thread.Sleep(800);
            lock (threadLock)
            {
                pool[poolIndex].in_use = false;
            }
        }
    }

    class Client
    {
        private Server server;
        public event EventHandler<ProcEventArgs> Request;

        public Client(Server server)
        {
            this.server = server;
            this.Request += server.Proc;
        }

        public void Send(int id)
        {
            ProcEventArgs args = new ProcEventArgs { id = id };
            OnProc(args);
        }

        protected virtual void OnProc(ProcEventArgs e)
        {
            Request?.Invoke(this, e);
        }
    }

    public class ProcEventArgs : EventArgs
    {
        public int id { get; set; }
    }
}