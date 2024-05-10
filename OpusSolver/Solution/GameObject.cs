using System.Collections.Generic;
using System.Linq;

namespace OpusSolver
{
    /// <summary>
    /// Represents an object or group of objects on the hex grid.
    /// </summary>
    public class GameObject
    {
        public Vector2 Position { get; set; }
        public int Rotation { get; set; }

        private GameObject m_parent;

        public GameObject Parent
        {
            get { return m_parent; }
            set
            {
                m_parent?.m_children.Remove(this);
                m_parent = value;
                m_parent?.m_children.Add(this);
            }
        }

        private List<GameObject> m_children = new List<GameObject>();

        public IEnumerable<GameObject> Children
        {
            get { return m_children; }
        }

        public GameObject(GameObject parent, Vector2 position, int rotation)
        {
            Parent = parent;
            Position = position;
            Rotation = rotation;
        }

        public Vector2 GetWorldPosition()
        {
            var pos = Position;
            var parent = Parent;
            while (parent != null)
            {
                pos += parent.Position;
                parent = parent.Parent;
            }

            return pos;
        }

        public IEnumerable<GameObject> GetAllObjects()
        {
            return new[] { this }.Concat(m_children.SelectMany(obj => obj.GetAllObjects()));
        }

        public void Remove()
        {
            Parent = null;
        }
    }
}
