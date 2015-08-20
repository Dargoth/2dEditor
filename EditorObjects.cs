using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Input = Microsoft.DirectX.DirectInput;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using System.Windows.Input;

namespace Editor.EditorObjects {
    /// <summary>
    /// Class represents Vertex - 2D-space point. Needed to generate Edges.
    /// </summary>
    public class Vertex {
        static EditorWindow window;
        public static readonly Point center = new Point(4, 4);
        public static Texture img { get; private set; }
        static Vertex from;
        static Vertex to;
        static bool edging;         // Allows us to create only one edge at time

        EditorFrame parent;
        public Point position{ get; set; }
        public Color color { get; private set; }
        private bool focused;
        private bool draged;
        public bool marked { get; private set; }

        static Vertex() {
            window = Program.window;
            img = TextureLoader.FromFile(window.device, "square7.png");
        }

        public Vertex(EditorFrame parent, Point position){
            this.parent = parent;
            this.color = Color.White;
            this.position = position;
        }

        public override bool Equals(object obj) {
            Vertex that = (Vertex)obj;
            if (this.position.X == that.position.X && this.position.Y == that.position.Y) {
                return true;
            }
            return false;
        }
        public override int GetHashCode() {
            return position.GetHashCode();
        }

        public void Update() {
            byte[] buttons = window.mouse.CurrentMouseState.GetMouseButtons();
            // Draging vertex and drop it if we should
            if (draged) {
                if (buttons[0] == 0) {
                    Drop();
                }
                position = window.mousePos; 
            }
            if (edging) {
                if (buttons[1] == 0) {
                    edging = false;
                }
            }
            // Checking if vertex overlaped by mouse. Waiting for action
            if ((this.position.X - 4 < window.mousePos.X && window.mousePos.X < this.position.X + 4) &&
                (this.position.Y - 4 < window.mousePos.Y && window.mousePos.Y < this.position.Y + 4)) {
                OnFocus();

                if (buttons[0] != 0) {
                    Pick();       
                }
                if (buttons[2] != 0) {
                    Delete();
                }
                if (buttons[1] != 0) {
                    CreateEdge();
                }
            }
            // Loosing focus
            else { if (focused){
                if (this == from || this == to) { color = Color.LightSeaGreen; }
                else { color = Color.White; }
                focused = false;
            }}
        }

        void OnFocus() {
            color = Color.Gold;
            focused = true;
        }

        void Pick() {
            if (!parent.draging) {
                parent.draging = true;
                draged = true;
            }
        }

        void Drop() {
            draged = false;
            parent.draging = false;
        }

        void Delete() { 
            marked = true; 
        }

        void CreateEdge() {
            if (!edging) {
                edging = true;
                if (from == null) {
                    from = this;
                }
                else if (from == this) { return; }
                else {
                    to = this;
                    parent.createEdge(from, to);
                    to = null;
                    from.focused = true;
                    from = null;
                }
            }
        }

        public Vector2 toVector2() {
            return new Vector2(position.X, position.Y);
        }
    }

    /// <summary>
    /// Class represents Edge - line which joins two Vertices.
    /// </summary>
    public class Edge {
        static EditorWindow window;
        public Vertex start {get; private set;}
        public Vertex end {get; private set;}
        public Color color { get; private set; }
        bool focused;
        public bool marked { get; private set; }

        static Edge() {
            window = Program.window;
        }

        public Edge(Vertex start, Vertex end) {
            this.start = start;
            this.end = end;
            color = Color.White;
        }

        public void Update() {

            if (onLine(start.position, end.position, window.mousePos)){
                byte[] buttons = window.mouse.CurrentMouseState.GetMouseButtons();
                OnFocus();
                if (buttons[2] != 0) {
                    Delete();
                }
            }
            else {
                if (focused) {
                    color = Color.White;
                    focused = false;
                }
            }
        }

        bool onLine(Point A, Point B, Point M) {
            Vector2 AB = new Vector2(B.X - A.X, B.Y - A.Y);
            Vector2 AM = new Vector2(M.X - A.X, M.Y - A.Y);
            Vector2 MA = new Vector2(A.X - M.X, A.Y - M.Y);
            Vector2 MB = new Vector2(B.X - M.X, B.Y - M.Y);
            float SP = AB.X * AM.Y - AM.X * AB.Y;
            float DP = MA.X * MB.X + MA.Y * MB.Y;
            if ((-1000 <= SP && SP <= 1000) && DP <= 0) return true;
            return false;
        }

        void OnFocus() {
            color = Color.Gold;
            focused = true;
        }

        void Delete() {
            marked = true;
        }

        public override bool Equals(object obj) {
            Edge that = (Edge) obj;
            if ((this.start == that.start || this.start == that.end) &&
                (this.end == that.start || this.end == that.end)) {
                return true;
            }
            else {
                return false;
            }
        }

        public override int GetHashCode() {
            return (start.GetHashCode() + end.GetHashCode());
        }
    }

    /// <summary>
    /// Contains Vertices and Edges, manages their drawing.
    /// </summary>
    public class EditorFrame {
        static Sprite sprite;
        static Line line;
        public HashSet<Edge> edges;
        public List<Vertex> vertices;
        public bool draging;           // Allows us to drag only one vertex at time
        public bool creating;          // Allows us to create only one vertex at time

        static EditorFrame() {
            sprite = new Sprite(Program.window.device);
            line = new Line(Program.window.device);
        }

        public EditorFrame() {
            this.edges = new HashSet<Edge>();
            this.vertices = new List<Vertex>();
        }

        public EditorFrame(EditorFrame frame) {
            this.vertices = new List<Vertex>();
            this.edges = new HashSet<Edge>();
            foreach (Edge edge in frame.edges) {
                Vertex start = createVertex(edge.start);
                Vertex end = createVertex(edge.end);
                createEdge(start, end);
            }
            foreach (Vertex vertex in frame.vertices) {
                createVertex(vertex);
            }
        }

        public void DrawFrame() {
            // Drawing vertices
            sprite.Begin(SpriteFlags.None);
            foreach (Vertex vertex in vertices){
                vertex.Update();
                if (vertex.marked) { 
                    vertices.Remove(vertex);
                    break;
                }
                sprite.Draw2D(Vertex.img, Rectangle.Empty, Rectangle.Empty,
                        Vertex.center, 0f, vertex.position, vertex.color);
            }
            sprite.End();

            // Drawing edges
            line.Begin();
            foreach (Edge edge in edges) {
                edge.Update();
                if (!vertices.Contains(edge.start) || !vertices.Contains(edge.end)) {
                    edges.Remove(edge);
                    break;
                }
                if (edge.marked) {
                    edges.Remove(edge);
                    break;
                }
                Vector2[] coords = { edge.start.toVector2(), edge.end.toVector2() };
                line.Draw(coords, edge.color);
            }
            line.End();
        }

        public void createVertex() {
            if (!creating) {
                creating = true;
                vertices.Add(new Vertex(this, Program.window.mousePos));
            }
        }
        public Vertex createVertex(Vertex vertex) {
            Vertex clone = new Vertex(this, vertex.position);
            if (!vertices.Contains(clone)) {
                vertices.Add(clone);
                return clone;
            }
            else {
                int i = vertices.IndexOf(clone);
                return vertices[i];
            }
        }
        public Vertex loadVertex(int X, int Y) {
            Vertex clone = new Vertex(this, new Point (X, Y));
            if (!vertices.Contains(clone)) {
                vertices.Add(clone);
                return clone;
            }
            else {
                int i = vertices.IndexOf(clone);
                return vertices[i];
            }
        }
        public void createEdge(Vertex start, Vertex end) {
            Edge edge = new Edge(start, end);
            bool added = edges.Add(edge);
            // Remove edge if it already exists
            if (!added) {
                edges.Remove(edge);
            }
        }
    }
}
