using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace single_cycle_cpu
{
    class Program
    {
        // this class used for register file
        public static class RegisterFile
        {
            public static int[] Registers = new int[16];
        }

        // this class used for memory
        public static class Memory
        {
            public static int[] datas = new int[8193];
        }

        //function for reading from file
        static void read_from_file(string[] machinCodes, ref int lineCount)
        {
            string line;
            if (File.Exists("file.txt"))
            {
                // here we have to give the file name which contains machine codes
                FileStream fileStream = new FileStream("file.txt", FileMode.Open, FileAccess.Read);
                using (StreamReader sr = new StreamReader(fileStream, Encoding.UTF8))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        machinCodes[lineCount++] = line;
                    }
                }
            }
            else
                Console.WriteLine("file.txt" + " Does not exist");
            

        }

        // function for finding the type of instruction
        static string check_type(string op)
        {

            RTdecoder rtd = new RTdecoder();
            ITdecoder itd = new ITdecoder();
            JTdecoder jtd = new JTdecoder();

            // checking if the instruction is R type
            foreach (KeyValuePair<string, string> item in rtd.instructions)
            {
                if (op == item.Key)
                    return "r";
            }

            // checking if the instruction is I type
            foreach (KeyValuePair<string, string> item in itd.instructions)
            {
                if (op == item.Key)
                    return "i";
            }

            // checking if the instruction is J type
            foreach (KeyValuePair<string, string> item in jtd.instructions)
            {
                if (op == item.Key)
                    return "j";
            }
            // what about directives? how to find out them?
            return "";
        }
        static void Main(string[] args)
        {
            //this part is for checking read_from_file funtion
            String[] machineCodes = new string[20];
            int lineCount = 0;

            read_from_file(machineCodes, ref lineCount);

            //initializing the register file
            Array.Clear(RegisterFile.Registers, 0, 16);

            //initializing the memory
            Array.Clear(Memory.datas, 0, 8193);

            //checking RTdecoder class if it works correctly
            if (File.Exists("file.txt"))
            {
                string mc = machineCodes[0];
                RTdecoder rtd = new RTdecoder();
                rtd.calc(mc);
            }
            else
                Console.WriteLine("file.txt" + " Does not exist! So we can not decode any thing!");
            

        }
    }
    public class RTdecoder
    {
        public Dictionary<string, string> instructions = new Dictionary<string, string>
        { { "0000","add" }, { "0001","sub" }, {"0010", "slt" }, {"0011", "or" }, {"0100", "nand"} };
        public string op = "";
        public int rs = -1;
        public int rt = -1;
        public int rd = -1;
        public void calc(string machineCode)
        {
            op = machineCode.Substring(4, 4);
            //binary to int
            rs = Convert.ToInt32(machineCode.Substring(8, 4), 2);
            Console.WriteLine(rs);
            rt = Convert.ToInt32(machineCode.Substring(12, 4), 2);
            Console.WriteLine(rt);
            rd = Convert.ToInt32(machineCode.Substring(16, 4), 2);
            Console.WriteLine(rd);
        }

    }

    public class ITdecoder
    {
        public Dictionary<string, string> instructions = new Dictionary<string, string>
        { {"0101","addi" }, {"0111","ori" }, {"0110","slti" }, {"1000","lui" }, {"1001","lw" },
            { "1010","sw" },{ "1011","beq" }, {"1100","jalr" } };
        public string op = "";
        public int rs = -1;
        public int rt = -1;
        public int offset = 66000;
        public void clac(string machineCode)
        {
            op = machineCode.Substring(4, 4);
            rs = Convert.ToInt32(machineCode.Substring(8, 4), 2);
            Console.WriteLine(rs);
            rt = Convert.ToInt32(machineCode.Substring(12, 4), 2);
            Console.WriteLine(rt);
            // offset can be negative or positive
        }
    }
    public class JTdecoder
    {
        public Dictionary<string, string> instructions = new Dictionary<string, string>
        { {"1101","j"}, {"1110" ,"halt"} };
        public string op = "";
        public int target = 66000;
        public void calc(string machineCode)
        {
            op = machineCode.Substring(4, 4);
            // target can be negative or positive
        }
    }
}

