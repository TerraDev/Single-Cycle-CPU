using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Shapes = System.Windows.Shapes;
using System.Speech.Synthesis;

namespace Single_Cycle_CPU
{
    public partial class MainWindow : Window
    {
        MUXA mux_a = new MUXA();
        static Memo memo = new Memo();
        MUXB mux_b = new MUXB();
        Register Regs = new Register();
        Jump_System js = new Jump_System();

        public SpeechSynthesizer speech;
        static int pc = 0;
        int lineNum = 0;
        bool first_time = true;

        public MainWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            speech = new SpeechSynthesizer();
            show_values(Regs);
            Speak("Welcome. Please write machine code in the textbox above and click -Run- or pick a text file with machine code.");
        }

        private void Text_Submit(object sender, RoutedEventArgs e)
        {
            if ((lineNum <= pc || pc == -1) && !first_time)
            {
                if ((sender as Button).IsEnabled)
                {
                    Speak("End of Program. Thank you for using our Software!");
                    PC_Box.Text = "PC = -1";
                    return;
                }
            }
            first_time = false;
            String[] k = GetLines(InputBox, ref lineNum);
            cycle_NO++;
            if (k[0] == "-1")
            {
                pc++;
                return;
            }

            Perform_instructions(k);

        }

        public void Perform_instructions(String[] texts)
        {
            Color_all_lines_Black();
            Color_all_Registers_White();
            Instruction ins = new Instruction(texts[0], texts[1], texts[2], texts[3], texts[4]);
            mux_a.Set_State(ins);
            memo.Set_memory_Signals(ins);
            mux_b.Set_State(ins);
            Regs.Set_State(ins);
            js.Set_states(ins);                                                                         //important
            Deploy_signals(mux_a, memo, mux_b, ins, Regs, js);


            js.increment_PC(ins);
            Throw_all_exceptions();
        }

        internal void Deploy_signals(Multiplexer a, Memo m, Multiplexer b, Instruction i, Register Regs, Jump_System js)
        {
            ALU_x alu = new ALU_x();
            int After_alu = 0;
            int mem_value = 0;
            int after_mux_B;
            Color_Path(i.rs, Brushes.Red);

            Color_Path("rs_to_ALU", Brushes.Red);
            if (a.State)                                                                                //Extension_Or_Reg; -> true=>Ext
            {
                Color_Path("Ext1", Brushes.Red);
                Color_Path("Ext2", Brushes.Red);
                Color_Path("Ext3", Brushes.Red);
                Color_Path("Ext4", Brushes.Red);
                After_alu = alu.Perform_Operation(i, Regs.Reg[i.rs], i.offset, Operation_textblock);
            }
            else                                                                                        //Extension_Or_Reg; -> false=>Register
            {
                Color_Path(i.rt, Brushes.Blue);
                Color_Path("rt_to_ALU", Brushes.Blue);
                After_alu = alu.Perform_Operation(i, Regs.Reg[i.rs], Regs.Reg[i.rt], Operation_textblock);
            }

            Color_Path("MuxA_to_ALU", Brushes.Red);
            Color_Path("ALU_to_Mem", Brushes.Red);

            if (m.signal_read)                                                                          // signal_read = i.Mem_Read;
            {
                Color_Path("mr1", Brushes.LightGreen);
                Color_Path("mr2", Brushes.LightGreen);
                Color_Path("mr3", Brushes.LightGreen);
                Color_Path("mem_access", Brushes.Red);
                mem_value = m.mem[After_alu];

                Memo_use++;
            }

            if (m.signal_write)                                                                           // signal_Write = i.Mem_Write;
            {
                Color_Path("mw1", Brushes.Pink);
                Color_Path("mw2", Brushes.Pink);
                Color_Path("mw3", Brushes.Pink);
                Color_Path("mem_access", Brushes.Red);
                Color_Path(i.rt, Brushes.SeaGreen);
                Color_Path("rt_to_mem1", Brushes.SeaGreen);
                Color_Path("sw(rt)", Brushes.SeaGreen);
                m.mem[After_alu] = Regs.Reg[i.rt];
                Memo_use++;
            }

            if (b.State)                                                             // State = i.Mem_to_Reg_Or_Reg_to_Reg; ->true=>mem_to_register
            {
                after_mux_B = mem_value;
                Color_Path("mtr1", Brushes.LightGreen);
                Color_Path("mtr2", Brushes.LightGreen);
                Color_Path("b", Brushes.LightGreen);
            }
            else                                                               // State = i.Mem_to_Reg_Or_Reg_to_Reg; ->false=>register_to_register
            {
                after_mux_B = After_alu;
                Color_Path("WB1", Brushes.Red);
                Color_Path("WB2", Brushes.Red);
                Color_Path("b", Brushes.Red);
            }

            if (Regs.state_wb)                                                                                  //state_wb = i.WB;
            {

                Color_Path("wwbb", Brushes.Gold);

                if (Regs.Ov_register)                                //for writing to rd                      Ov_register = i.R_type;
                {
                    Color_Path("reg_write", Brushes.Gold);
                    Regs.Reg[i.rd] = after_mux_B;
                    Color_Register(i.rd, Brushes.Gold);
                }
                else if (i.Opcode != "1100")                  //for anything except jalr
                {
                    //writing to rt                        // if (i.Opcode != "1011")    
                    Color_Path("reg_write", Brushes.Gold); // if it is beq the value of register must not change
                    Regs.Reg[i.rt] = after_mux_B;          // -> watch out for jalr ->watch out for anything!
                    Color_Register(i.rt, Brushes.Gold);
                }
                else                                       // for jalr
                {
                    Regs.Reg[i.rt] = pc + 1;
                    Color_Register(i.rt, Brushes.Gold);
                }
            }

            show_values(Regs);
            Speak($"instruction {pc} successfully operated.");

            if (js.Jump)
            {
                Color_Path("j1", Brushes.Pink);
                Color_Path("j2", Brushes.Pink);
                Color_Path("j3", Brushes.Pink);

                if (i.Opcode == "1011")
                {
                    js.jump(i, i.offset, after_mux_B == 0);
                }
                else
                {
                    js.jump(i, after_mux_B, true);
                }
            }

        }

        public void Color_Path(String id, SolidColorBrush cl)
        {
            ((Shapes.Path)GetByUid(root_grid, id)).Stroke = cl;
        }

        public void Color_Path(int id, SolidColorBrush cl)
        {
            String i = Convert.ToString(id);
            ((Shapes.Path)GetByUid(root_grid, i)).Stroke = cl;
        }

        public void Color_all_lines_Black()
        {
            for (int i = 0; i < 16; i++)
            {
                ((Shapes.Path)GetByUid(root_grid, Convert.ToString(i))).Stroke = Brushes.Black;
            }
            Set_Color_Black_By_UID(new string[]{ "Ext1", "Ext2", "Ext3", "Ext4" , "rt_to_ALU",
            "rt_to_mem1", "rs_to_ALU","MuxA_to_ALU","mr1","mr2","mr3", "mw1" , "mw2" , "mw3",
            "sw(rt)", "mem_access", "ALU_to_Mem", "WB1","WB2", "mtr1","mtr2","b","reg_write",
            "wwbb","j1","j2","j3"});
        }

        public void Color_all_Registers_White()
        {
            for (int i = 0; i < 16; i++)
            {
                ((TextBlock)GetByUid(root_grid, "a" + Convert.ToString(i))).Background = Brushes.White;
            }
        }

        public void Color_Register(int id, SolidColorBrush cl)
        {
            String i = "a" + Convert.ToString(id);
            ((TextBlock)GetByUid(root_grid, i)).Background = cl;
        }

        //TODO: create method to change color to red. Parameter: Instruction
        public string[] GetLines(TextBox textBox, ref int lineNum)
        {
            String lines = InputBox.Text;
            String[] h = lines.Split(' ', '\n', '\r');
            string[] line = ReadLines(h);
            lineNum = line.Length;

            line = Convert_to_binary(line);
            string[] fields = decode(line[pc]);
            return fields;
        }
        public string[] ReadLines(string[] h)
        {
            List<string> lines = new List<string>();
            for (int i = 0; i < h.Length; i++)
            {
                if (h[i] != "")
                    lines.Add(h[i]);
            }
            return lines.ToArray();
        }
        // function for converting decimal or hex to binary 
        public string[] Convert_to_binary(string[] s)
        {
            int size = s.Length;
            for (int i = 0; i < size; i++)
            {
                string binary = "", decValue = "";
                int bSize = 0;

                if (s[i].Contains("0x"))  //for hex
                {

                    if (s[i].Length >= 4 && !s[i].Contains("-"))
                    {
                        binary = Convert.ToString(Convert.ToInt32(s[i], 16), 2);//first convert hex to decimal then convert decimal to binary
                        bSize = binary.Length;
                        string zero = "";
                        for (int j = 0; j < (32 - bSize); j++)
                            zero += "0";
                        s[i] = zero + binary;
                    }
                    else // for directive .fill
                    {
                        string hexValue = "";
                        string sign = "";

                        if (s[i].Contains("-"))      //if number is negative
                        {
                            hexValue = s[i].Substring(1, s[i].Length - 1);
                            sign += "-";
                        }
                        else                        //if number is positive
                            hexValue = s[i];

                        decValue = Convert.ToString(Convert.ToInt32(hexValue, 16)); // decimal
                        s[i] = "D" + sign + decValue;
                    }

                    Console.WriteLine(s[i]);
                }
                                else if (s[i].Contains("0b")) //for binary
                                {

                                    if (s[i].Length >= 13 && !s[i].Contains("-"))
                                    {
                                        binary = s[i].Substring(2, s[i].Length - 2);
                                        bSize = binary.Length;
                                        string zero = "";
                                        for (int j = 0; j < (32 - bSize); j++)
                                            zero += "0";
                                        s[i] = zero + binary;
                                    }
                                    else // for directive .fill
                                    {
                                        string sign = "";

                                        if (s[i].Contains("-"))      //if number is negative
                                        {
                                            binary = s[i].Substring(3, s[i].Length - 3);
                                            sign += "-";
                                        }
                                        else                        //if number is positive
                                            binary = s[i].Substring(2, s[i].Length - 2);

                                        decValue = Convert.ToString(Convert.ToInt32(binary, 2));
                                        s[i] = "D" + sign + decValue;
                                    }

                                    Console.WriteLine(s[i]);
                                }
                                
                                else                                //for decimal -> that is not hex and that is not binary then it is decimal so convert decimal to binary
                                {
                                    int num = Convert.ToInt32(s[i]);
                                    if (num >= 4096)
                                    {
                                        binary = Convert.ToString(Convert.ToInt32(s[i]), 2);//convert decimal to binary
                                        bSize = binary.Length;
                                        string zero = "";
                                        for (int j = 0; j < (32 - bSize); j++)
                                            zero += "0";
                                        s[i] = zero + binary;
                                    }
                                    else // for directive .fill
                                    {
                                        s[i] = "D" + s[i];
                                    }

                                    Console.WriteLine(s[i]);
                                }
                                
            }
            return s;
        }
        public string[] decode(string line)
        {
            RTdecoder rtd = new RTdecoder();
            ITdecoder itd = new ITdecoder();
            JTdecoder jtd = new JTdecoder();
            string[] fields = new string[6];

            if (line.Contains("D"))
            {
                fields = directive_calc(line);
            }
            else
            {
                string type = check_type(line.Substring(4, 4), ref rtd, ref itd, ref jtd);

                if (type == "r")
                {
                    fields = rtd.calc(line);
                }
                else if (type == "i")
                {
                    fields = itd.clac(line);
                }
                else if (type == "j")
                {
                    fields = jtd.calc(line);
                }
            }

            return fields;
        }
        static string[] directive_calc(string num)
        {
            string[] f = { "-1", "-1", "-1", "-1", "-1", "-1" };
            memo.mem[pc] = Convert.ToInt32(num.Substring(1, num.Length - 1));
            Memo_use++;
            return f;
        }
        // function for finding the type of instruction
        static string check_type(string op, ref RTdecoder rtd, ref ITdecoder itd, ref JTdecoder jtd)
        {

            // checking if the instruction is R type
            foreach (KeyValuePair<string, string> item in rtd.instructions)
            {
                if (op == item.Key)
                {
                    return "r";
                }
            }

            // checking if the instruction is I type
            foreach (KeyValuePair<string, string> item in itd.instructions)
            {
                if (op == item.Key)
                {
                    return "i";
                }
            }

            // checking if the instruction is J type
            foreach (KeyValuePair<string, string> item in jtd.instructions)
            {
                if (op == item.Key)
                {
                    return "j";
                }
            }
            // what about directives? how to find out them?
            return "";
        }

        public class RTdecoder
        {
            public Dictionary<string, string> instructions = new Dictionary<string, string>
            { { "0000","add" }, { "0001","sub" }, {"0010", "slt" }, {"0011", "or" }, {"0100", "nand"} };
            public string op = "";
            public int rs = -1;
            public int rt = -1;
            public int rd = -1;
            public string[] calc(string machineCode)
            {
                op = machineCode.Substring(4, 4);
                //binary to int
                rs = Convert.ToInt32(machineCode.Substring(8, 4), 2);
                Console.WriteLine(rs);
                rt = Convert.ToInt32(machineCode.Substring(12, 4), 2);
                Console.WriteLine(rt);
                rd = Convert.ToInt32(machineCode.Substring(16, 4), 2);
                Console.WriteLine(rd);

                // upadating the register usage
                Usage_of_Regs[rs] += 1;
                Usage_of_Regs[rt] += 1;
                Usage_of_Regs[rd] += 1;
                total_usage += 3;

                string[] fields = {op, Convert.ToString(rs), Convert.ToString(rt), Convert.ToString(rd),
                    machineCode.Substring(20,12), "" };//20 12
                return fields;
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
            public string[] clac(string machineCode)
            {
                op = machineCode.Substring(4, 4);
                rs = Convert.ToInt32(machineCode.Substring(8, 4), 2);
                Console.WriteLine(rs);
                rt = Convert.ToInt32(machineCode.Substring(12, 4), 2);
                Console.WriteLine(rt);
                // offset can be negative or positive
                if (machineCode.Substring(16, 1) == "1")
                {
                    offset = -1 * Convert.ToInt32(two_comp(machineCode.Substring(16, 16)), 2);
                }
                else
                {
                    offset = Convert.ToInt32(machineCode.Substring(16, 16), 2);
                }

                // upadating the register usage
                //                if (op != "1000")//lui
                //                {
                Usage_of_Regs[rs] += 1;
                total_usage += 1;//if it is not lui instruction so we have to increase the value of total_usage because we have rs field too.
                                 //               }
                Usage_of_Regs[rt] += 1;
                total_usage += 1;

                string[] fields = { op, Convert.ToString(rs), Convert.ToString(rt), "65535", Convert.ToString(offset), "" };
                return fields;
            }
        }
        public class JTdecoder
        {
            public Dictionary<string, string> instructions = new Dictionary<string, string>
            { {"1101","j"}, {"1110" ,"halt"} };
            public string op = "";
            public int target = 66000;
            public string[] calc(string machineCode)
            {
                op = machineCode.Substring(4, 4);
                // target can be negative or positive
                if (machineCode.Substring(16, 1) == "1")
                {
                    target = -1 * Convert.ToInt32(two_comp(machineCode.Substring(16, 16)), 2);
                }
                else
                {
                    target = Convert.ToInt32(machineCode.Substring(16, 16), 2);
                }
                string[] fields = { op, machineCode.Substring(8, 4), machineCode.Substring(12, 4), "65535", Convert.ToString(target), "" };

                Usage_of_Regs[0]++;

                return fields;
            }
        }

        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        public static string two_comp(string offset)
        {
            offset = Reverse(offset);
            int cnt = 0;
            string result = "";
            for (int i = 0; i < 16; i++)
            {
                if (cnt < 1)
                {
                    result += offset[i];
                    if (offset[i] == '1')
                        cnt++;
                }
                else
                {
                    if (offset[i] == '1')
                        result += '0';
                    else if (offset[i] == '0')
                        result += '1';
                }
            }

            return Reverse(result);
        }


        // end of decoder


        public static UIElement GetByUid(DependencyObject rootElement, string uid)
        {
            foreach (UIElement element in LogicalTreeHelper.GetChildren(rootElement).OfType<UIElement>())
            {
                if (element.Uid == uid)
                    return element;
                UIElement resultChildren = GetByUid(element, uid);
                if (resultChildren != null)
                    return resultChildren;
            }
            return null;
        }

        public void Set_Color_Black_By_UID(string[] uid)
        {
            foreach (string k in uid)
            {
                foreach (UIElement element in root_grid.Children)
                {
                    if (element.Uid == k)
                    {
                        ((Shapes.Path)(element)).Stroke = Brushes.Black;
                        break;
                    }

                }
            }
        }

        /// ///////////////////////////////////////////////////////Logical Circuit stuff
        /// Multiplexers:
        public abstract class Multiplexer
        {
            public bool State;

            public abstract void Set_State(Instruction i);
        }

        public class MUXA : Multiplexer
        {
            public override void Set_State(Instruction i)
            {
                State = i.Extension_Or_Reg;
            }
        }

        public class MUXB : Multiplexer
        {
            public override void Set_State(Instruction i)
            {
                State = i.Mem_to_Reg_Or_Reg_to_Reg;
            }
        }

        internal class Memo
        {
            public bool signal_read;
            public bool signal_write;
            public int[] mem = new int[65535];

            public Memo()
            {
                mem[0] = -1;
            }

            public void Set_memory_Signals(Instruction i)
            {
                signal_read = i.Mem_Read;
                signal_write = i.Mem_Write;
            }
        }

        internal class Register
        {
            public Register()
            {
                Random r = new Random();
                Reg = new int[16];
                for (int i = 1; i < 16; i++)
                {
                    Reg[i] = r.Next(-100 , 100);
                    //Reg[i] = r.Next(0, 6);
                }
            }
            internal bool state_wb;
            internal bool Ov_register;
            internal int[] Reg;
            public void Set_State(Instruction i)
            {
                state_wb = i.WB;
                Ov_register = i.R_type;
            }
        }

        internal void show_values(Register r)
        {
            for (int i = 0; i < 16; i++)
            {
                ((TextBlock)GetByUid(root_grid, "a" + Convert.ToString(i))).Text = Convert.ToString(r.Reg[i]);
            }
            PC_Box.Text = "PC =" + pc;
        }

        class ALU_x
        {

            public int Perform_Operation(Instruction i, int i1, int i2, TextBlock T)
            {
                if (i.Opcode == "0000" || i.Opcode == "0101" || i.Opcode == "1001" || i.Opcode == "1010" || i.Opcode == "1100" || i.Opcode == "1101" || i.Opcode == "1110")
                {
                    T.Text = "ADD";
                    return (i1 + i2);
                }

                else if (i.Opcode == "0001" || i.Opcode == "1011")//beq
                {
                    T.Text = "SUB";
                    return (i1 - i2);
                }

                else if (i.Opcode == "0010" || i.Opcode == "0110")
                {
                    T.Text = "LESS THAN";
                    if (i1 < i2) return 1;
                    else return 0;
                }

                else if (i.Opcode == "0011" || i.Opcode == "0111")
                {
                    T.Text = "OR";
                    return i1 | i2;
                }

                else if (i.Opcode == "0100")
                {
                    T.Text = "NAND";
                    return ~(i1 & i2);
                }
                else if (i.Opcode == "1000")     //lui
                {
                    T.Text = "LEFT SHIFT 16 TIMES";
                    return (i2) << 16;     // always i1 = 0
                }
                else throw new Exception("Couldn't define ALU operation");
            }
        }

        internal class Jump_System
        {
            internal bool Jump;

            public void jump(Instruction i, int jvalue, bool will_jump)
            {
                if (i.Opcode == "1011")                                                    //beq
                {
                    if (will_jump)
                    {
                        pc += jvalue;
                    }
                }
                else if (i.Opcode == "1101" || i.Opcode == "1110" || i.Opcode == "1100")     //j & halt & jalr
                {
                    pc = jvalue;
                }
            }

            public void increment_PC(Instruction i)
            {
                if (i.Opcode != "1101" && i.Opcode != "1110" && i.Opcode != "1100")
                {
                    pc++;
                }
            }
            internal void Set_states(Instruction i)
            {
                this.Jump = i.Jump;
            }
        }
        /// //////////////////////////////////////////////////////////////////////////////


        private void Browse_Clicker(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            //dlg.DefaultExt = ".txt";
            dlg.Filter = "Text (*.txt) | *.txt";

            if (dlg.ShowDialog() == true)
            {
                // Open document 
                InputBox.Text = dlg.FileName;
                InputBox.Text = System.IO.File.ReadAllText(dlg.FileName);
                Speak("Files loaded successfully!");
            }
            else
                Speak("Failed to load files!");
        }

        private void Speak(string txt)
        {
            X_REPORTER.Text = txt;

            // Speak
            speech.SpeakAsync(txt);

            // 0 to 100
            speech.Volume = 100;

            // -10 to 10
            speech.Rate = 0;
        }

        internal void Throw_all_exceptions()
        {
            if (Regs.Reg[0] != 0)
                throw new Exception("CANNOT CHANGE VALUE OF REG 0");
            if (memo.mem[0] != -1)
                throw new Exception("CANNOT CHANGE VALUE OF MEMO INDEX 0");
            if (pc > lineNum || pc < -1)
                throw new Exception("Jumped out of Instuction border");

            //come up with other exceptions maybe?
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Usages u = new Usages();

            foreach (KeyValuePair<int, int> kvp in Usage_of_Regs)
            {
                u.Usage_textblock.Text += "Reg " + kvp.Key + "  =>  " + Math.Round(((double)kvp.Value * 100) / total_usage, 2) + "%\n";
            }

            u.Usage_textblock.Text += "----------------------------------------------------\n";
            u.Usage_textblock.Text += "Total Number of instructions(lines) : " + lineNum + '\n';
            u.Usage_textblock.Text += "Line Number of current instruction : " + pc + '\n';
            u.Usage_textblock.Text += "Number of cycles performed : " + cycle_NO + '\n';
            u.Usage_textblock.Text += "Memory usage  =>  " + ((double)Memo_use * 100 / cycle_NO) + "%\n";

            u.Show();
        }

        public static int total_usage = 0;
        public static Dictionary<int, int> Usage_of_Regs = new Dictionary<int, int>()
        {
            [0] = 0,
            [1] = 0,
            [2] = 0,
            [3] = 0,
            [4] = 0,
            [5] = 0,
            [6] = 0,
            [7] = 0,
            [8] = 0,
            [9] = 0,
            [10] = 0,
            [11] = 0,
            [12] = 0,
            [13] = 0,
            [14] = 0,
            [15] = 0
        };

        public static int cycle_NO;
        public static int Memo_use;

        private void Restart_Clicker(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }
    }
}