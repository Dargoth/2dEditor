using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Input = Microsoft.DirectX.DirectInput;
using System;
using System.Windows.Forms;
using Editor.EditorObjects;
using System.Drawing;
using System.Collections;
using System.Windows.Input;
using System.Collections.Generic;
using System.Xml;
using System.Text;


namespace Editor {
    public partial class EditorWindow : Form {
        public const int ScreenWidth = 800;
        public const int ScreenHeight = 600;
        public Color BGcolor = Color.FromArgb(255, 70, 70, 70);
        const string helpMessage = "To show/hide help press 'F1'\n" +
                                          "To save frames press 'Q'\n" +
                                          "To load frames press 'E'\n" +
                                          "To create empty frame press 'S'\n" +
                                          "To create frame from current press 'W'\n" +
                                          "To delete frame press 'X'\n" +
                                          "To switch to next frame press 'D'\n" +
                                          "To switch to pevious frame press 'A'\n" +
                                          "To create vertex in cursor position press\n    'Space bar'\n" +
                                          "To delete vertex or edge click\n    'Middle mouse button' on it\n" +
                                          "To drag vertex use 'Left mouse button'\n    on it\n" +
                                          "To create edge click 'Right mouse button'\n    on two vertices\n";
        public Device device;
        public Input.Device keyboard;
        public Input.Device mouse;
        public Point mousePos;
        Microsoft.DirectX.Direct3D.Font font;
        public List<EditorFrame> frames;
        public int frameIndex;
        public EditorFrame currentFrame;
        bool managingInput;
        bool showHelp = true;

        public EditorWindow() {
            InitializeComponent();
            InitGraphics();
            InitInput();

            frames = new List<EditorFrame>();
            frameIndex = -1;
            font = new Microsoft.DirectX.Direct3D.Font(device, 16, 8, FontWeight.Normal, 0, false, CharacterSet.Ansi, Precision.Default, FontQuality.Default, PitchAndFamily.DefaultPitch, "Arial");

        }
        
        void SaveFrames(){
            string filePath = "";
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK) {
                filePath = ofd.FileName;
            }
            else return;
            XmlWriter xmlWriter = XmlWriter.Create(filePath);
            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("frames");

            foreach (EditorFrame frame in frames) {
                xmlWriter.WriteStartElement("frame");
                xmlWriter.WriteStartElement("vertices");
                foreach (Vertex vertex in frame.vertices) {
                    xmlWriter.WriteStartElement("vertex");
                    xmlWriter.WriteAttributeString("X", vertex.position.X.ToString());
                    xmlWriter.WriteAttributeString("Y", vertex.position.Y.ToString());
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteStartElement("edges");
                foreach (Edge edge in frame.edges) {
                    xmlWriter.WriteStartElement("edge");
                    xmlWriter.WriteAttributeString("startX", edge.start.position.X.ToString());
                    xmlWriter.WriteAttributeString("startY", edge.start.position.Y.ToString());
                    xmlWriter.WriteAttributeString("endX", edge.end.position.X.ToString());
                    xmlWriter.WriteAttributeString("endY", edge.end.position.Y.ToString());
                    xmlWriter.WriteEndElement();
                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndElement();
            }
            xmlWriter.WriteEndElement();
            xmlWriter.Close();
        }
        void LoadFrames() {
            string filePath = "";
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK) {
                filePath = ofd.FileName;
            }
            else return;
            List<EditorFrame> loadedFrames = new List<EditorFrame>();
            EditorFrame loadedFrame;
            XmlReader xmlReader = XmlReader.Create(filePath);
            while (xmlReader.Read()) {
                loadedFrame = null;
                if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "frame")) {
                    loadedFrame = new EditorFrame();
                    xmlReader.Read();
                    if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "vertices")) {
                        XmlReader innerReader = xmlReader.ReadSubtree();
                        while (innerReader.Read()) {
                            if ((innerReader.NodeType == XmlNodeType.Element) && (innerReader.Name == "vertex")) {
                                if (innerReader.HasAttributes) {
                                    loadedFrame.loadVertex(int.Parse(innerReader.GetAttribute("X")), int.Parse(innerReader.GetAttribute("Y")));
                                }
                            }
                        }
                    }
                    xmlReader.Read();
                    if ((xmlReader.NodeType == XmlNodeType.Element) && (xmlReader.Name == "edges")) {
                        XmlReader innerReader = xmlReader.ReadSubtree();
                        while (innerReader.Read()) {
                            if ((innerReader.NodeType == XmlNodeType.Element) && (innerReader.Name == "edge")) {
                                if (innerReader.HasAttributes) {
                                    Vertex start = loadedFrame.loadVertex(int.Parse(innerReader.GetAttribute("startX")), int.Parse(innerReader.GetAttribute("startY")));
                                    Vertex end = loadedFrame.loadVertex(int.Parse(innerReader.GetAttribute("endX")), int.Parse(innerReader.GetAttribute("endY")));
                                    loadedFrame.createEdge(start, end);
                                }
                            }
                        }
                    }
                }
                if (loadedFrame != null) { loadedFrames.Add(loadedFrame); }
            }
            frames = loadedFrames;
        }

        public void InitGraphics() {
            PresentParameters presentParams = new PresentParameters();
            presentParams.SwapEffect = SwapEffect.Discard;
            presentParams.Windowed = true;
            device = new Device(0, DeviceType.Hardware, this, CreateFlags.SoftwareVertexProcessing, presentParams);
        }
        private void InitInput() {
            //create keyboard device.
            keyboard = new Input.Device(Input.SystemGuid.Keyboard);
            if (keyboard == null) {
                MessageBox.Show("No keyboard found.");
            }
            keyboard.SetCooperativeLevel(
            this,
            Input.CooperativeLevelFlags.NonExclusive |
            Input.CooperativeLevelFlags.Background);
            keyboard.Acquire();

            //create mouse device.
            mouse = new Input.Device(Input.SystemGuid.Mouse);
            if (mouse == null) {
                MessageBox.Show("No mouse found.");
            }
            mouse.Properties.AxisModeAbsolute = false;
            mouse.SetCooperativeLevel(
            this,
            Input.CooperativeLevelFlags.NonExclusive |
            Input.CooperativeLevelFlags.Background);
            mouse.Acquire();
        }

        public void Render() {
            mousePos = this.PointToClient(System.Windows.Forms.Cursor.Position);
            //Clear the backbuffer and begin the scene.
            device.Clear(ClearFlags.Target, BGcolor, 1.0f, 0);
            device.BeginScene();
            foreach (Input.Key key in keyboard.GetPressedKeys()) {
                switch (key) {
                    case Input.Key.Space:
                        frames[frameIndex].createVertex();
                        break;
                    case Input.Key.W:
                        createFrame(frames[frameIndex]);
                        break;
                    case Input.Key.A:
                        prevFrame();
                        break;
                    case Input.Key.D:
                        nextFrame();
                        break;
                    case Input.Key.X:
                        removeFrame();
                        break;
                    case Input.Key.S:
                        createFrame();
                        break;
                    case Input.Key.Q:
                        SaveFrames();
                        break;
                    case Input.Key.E:
                        LoadFrames();
                        break;
                    case Input.Key.F1:
                        if (!managingInput) {
                            managingInput = true;
                            if (showHelp) {
                                showHelp = false;
                            }
                            else {
                                showHelp = true;
                            }
                        }
                        break;
                }
            }
            if (keyboard.GetPressedKeys().Length == 0) {
                if (managingInput) { managingInput = false; }
                if (frames[frameIndex].creating) { frames[frameIndex].creating = false; }
            }
            frames[frameIndex].DrawFrame();
            font.DrawText(null, "Frame " + (frameIndex + 1) + " of " + frames.Count, Width - 125, 15, Color.LightGoldenrodYellow);
            if (showHelp) {
                font.DrawText(null, helpMessage, Width - 325, Height - 300, Color.LightGoldenrodYellow);
            }
    
            //End the scene.
            device.EndScene();
            device.Present();
        }

        public void createFrame() {
            if (!managingInput) {
                frameIndex++;
                EditorFrame frame = new EditorFrame();
                frames.Add(frame);
                managingInput = true;
            }
        }
        public void createFrame(EditorFrame frame) {
            if (!managingInput) {
                frameIndex++;
                frame = new EditorFrame(frame);
                frames.Add(frame);
                managingInput = true;
            }
        }

        private void removeFrame() {
            if (!managingInput) {
                managingInput = true;
                frames.Remove(frames[frameIndex]);
                if (frameIndex > 0) {
                    frameIndex--;
                    return;
                }
                else {
                    if (frames.Count == 0) {
                        managingInput = false;
                        createFrame();
                        frameIndex = 0;
                    }
                    else { frameIndex = 0; }
                }
                
            }
        }

        private void nextFrame() {
            if (!managingInput) {
                managingInput = true;
                if (frameIndex < frames.Count - 1) {
                    frameIndex++;
                }
            }
        }
        private void prevFrame() {
            if (!managingInput) {
                managingInput = true;
                if (frameIndex > 0) {
                    frameIndex--;
                }
            }
        }

    }
}
