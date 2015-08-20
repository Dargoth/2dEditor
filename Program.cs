using System;
using System.Drawing;
using System.Windows.Forms;

namespace Editor {

    static class Program {
        public static EditorWindow window;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            using (window = new EditorWindow()) {
                window.Show();
                window.createFrame();
                // While the form is still valid, render and process messages.
                while (window.Created) {
                    window.Render();
                    Application.DoEvents();
                }
            }
        }
    }
}
