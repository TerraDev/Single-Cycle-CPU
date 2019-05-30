using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Single_Cycle_CPU
{
    public class Instruction
    {
        public Instruction(String OP, String rd0, String rs0, String rt0, String offset0)
        {
            Opcode = OP;
            op1 = (Opcode[0] == '1');
            op2 = (Opcode[1] == '1');
            op3 = (Opcode[2] == '1');
            op4 = (Opcode[3] == '1');

            WB = !((op1 && op3) || (op1 && op2 && op4));
            Mem_Read = !op1 && op2 && op3 && !op4;
            Mem_Write = !op1 && op2 && !op3 && op4;
            Jump = (op1 && op2) || ((rs == rt) && (op1 && !op2 && op3 && op4));
            Extension_Or_Reg = (op1 || op2) && (op3 || op4);
            Mem_to_Reg_Or_Reg_to_Reg = Mem_Read;
            R_type=!((op1 || op2) && (op1 || op2 || op3));
            rd = Convert.ToInt32(rd0);
            rt = Convert.ToInt32(rt0);
            rs = Convert.ToInt32(rs0);
            offset = Convert.ToInt32(offset0);
        }

        public int rd { get; private set; }
        public int rt { get; private set; }
        public int rs { get; private set; }
        public String Opcode { get; private set; }
        public int offset { get; private set; }
        public short off { get; private set; }

        //opcodes
        public bool op1 { get; private set; }
        public bool op2 { get; private set; }
        public bool op3 { get; private set; }
        public bool op4 { get; private set; }

        //Gate controls
        public bool WB { get; private set; }
        public bool Mem_Read { get; private set; }
        public bool Mem_Write { get; private set; }
        public bool Jump { get; private set; }
        public bool R_type { get; private set; }

        //Multiplexer Controls
        public bool Extension_Or_Reg { get; private set; }
        public bool Mem_to_Reg_Or_Reg_to_Reg { get; private set; }


    }
}
