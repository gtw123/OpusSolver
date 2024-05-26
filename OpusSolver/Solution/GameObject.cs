using System.Collections.Generic;
using System.Linq;

namespace OpusSolver
{
    /// <summary>
    /// Represents an object or group of objects on the hex grid.
    /// </summary>
    public class GameObject
    {
        public Transform2D Transform { get; set; }

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

        public GameObject(GameObject parent, Vector2 position, HexRotation rotation)
            : this(parent, new Transform2D(position, rotation))
        {
        }

        public GameObject(GameObject parent, Transform2D transform)
        {
            Parent = parent;
            Transform = transform;
        }

        public Transform2D GetWorldTransform()
        {
            var transform = Transform;
            var parent = Parent;
            while (parent != null)
            {
                transform = parent.Transform.Apply(transform);
                parent = parent.Parent;
            }

            return transform;
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
