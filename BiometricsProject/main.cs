using Org.BouncyCastle.Crypto.Engines;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BiometricsProject
{
    delegate void Function();
    public partial class main : Form
    {

        private DPFP.Template Template;
        public main()
        {
            InitializeComponent();
        }

        private void OnTemplate(DPFP.Template template)
        {
            this.Invoke(new Function(delegate ()
            {
                Template = template; 

                if (Template != null)
                {
                    MessageBox.Show("The fingerprint template is ready for fingerprint verification", "Fingerprint Enrollment");
                }
                else
                {
                    MessageBox.Show("The FIngerprint template is not valid. Repeat fingerprint scanning", "Fingerprint Enrollment");
                }
            }));
        }

        private void enrol_btn_Click(object sender, EventArgs e)
        {
            enroll EnFrm = new enroll();
            EnFrm.OnTemplate += this.OnTemplate;
            EnFrm.Show();
        }

        private void verify_btn_Click(object sender, EventArgs e)
        {
            verify VeFrm = new verify();
            VeFrm.Verify(Template);
        }
    }
}
