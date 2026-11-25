using UnityEngine;

namespace SpaceAndCrafts.Gameplay
{
    /// <summary>
    /// Optional physical carry for large craft parts. Hook presentation later.
    /// </summary>
    public sealed class CarryableComponent : Interactable
    {
        [SerializeField] private Transform attachAnchor;
        [SerializeField] private Vector3 heldLocalPosition = new Vector3(0f, 1.2f, 0.8f);
        [SerializeField] private bool isHeld;

        private Transform _originalParent;
        private Rigidbody _body;

        public bool IsHeld => isHeld;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        protected override void HandleInteract(GameObject interactor)
        {
            if (isHeld)
            {
                Release();
                return;
            }

            PickUp(interactor.transform);
        }

        public void PickUp(Transform holder)
        {
            _originalParent = transform.parent;
            transform.SetParent(attachAnchor != null ? attachAnchor : holder, worldPositionStays: false);
            transform.localPosition = heldLocalPosition;
            transform.localRotation = Quaternion.identity;

            if (_body != null)
            {
                _body.isKinematic = true;
            }

            isHeld = true;
        }

        public void Release()
        {
            transform.SetParent(_originalParent, worldPositionStays: true);

            if (_body != null)
            {
                _body.isKinematic = false;
            }

            isHeld = false;
        }
    }
}
