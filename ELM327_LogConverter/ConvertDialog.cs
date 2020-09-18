using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ELM327_LogConverter {
	public partial class ConvertDialog : Form {
		public int Weight;
		public int Gear;

		public ConvertDialog() {
			InitializeComponent();
		}

		private void BtnOk_Click(object sender, EventArgs e) {
			if (!int.TryParse(tbWeight.Text, out Weight)) {
				Weight = 70;
			}

			if (!int.TryParse(tbGear.Text, out Gear)) {
				Gear = 2;
			}

			Close();
		}
	}
}
