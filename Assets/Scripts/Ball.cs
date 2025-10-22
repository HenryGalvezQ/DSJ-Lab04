using UnityEngine;

public class Ball : MonoBehaviour
{
	[HideInInspector] public Rigidbody2D rb;
	[HideInInspector] public CircleCollider2D col;

	[HideInInspector] public Vector3 pos { get { return transform.position; } }
	[HideInInspector] public bool isMoving = false;

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		col = GetComponent<CircleCollider2D>();
	}

	void FixedUpdate()
	{
		if (rb.IsSleeping() || rb.velocity.magnitude < 0.1f)
		{
			isMoving = false;
		}
		else
		{
			isMoving = true;
		}
	}

	// --- MÉTODO NUEVO ---
	// Se activa cuando la bola toca CUALQUIER trigger
	void OnTriggerEnter2D(Collider2D other)
	{
		// Si el trigger que tocamos tiene la etiqueta "Respawn"
		if (other.CompareTag("Respawn"))
		{
			Debug.Log("Bola fuera de límites. Forzando fin de turno.");

			// Le dice al GameManager que inicie el proceso de fin de turno
			// (esto contará como un tiro fallido)
			GameManager.Instance.ForzarFinDeTurno();
		}
	}

	public void Push(Vector2 force)
	{
		rb.AddForce(force, ForceMode2D.Impulse);
		isMoving = true;
	}

	public void ActivateRb()
	{
		rb.isKinematic = false;
	}

	public void DesactivateRb()
	{
		rb.velocity = Vector3.zero;
		rb.angularVelocity = 0f;
		rb.isKinematic = true;
		isMoving = false;
	}
}