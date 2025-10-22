using UnityEngine;
using UnityEngine.UI; // ¡Importante! Para UI estándar
using TMPro; // ¡Importante! Si usas TextMeshPro
using UnityEngine.SceneManagement; // Para reiniciar el juego

public class GameManager : MonoBehaviour
{
	#region Singleton
	/// <summary>
	/// Instancia estática del GameManager para acceso global.
	/// </summary>
	public static GameManager Instance;

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}
	#endregion

	[Header("Componentes de Juego")]
	public GameObject ballPrefab;   // El prefab de la bola
	public Transform spawnPoint;    // Punto de aparición de la bola
	public Trajectory trajectory;   // Script de la trayectoria

	[Header("Configuración de Lanzamiento")]
	[SerializeField] float pushForce = 4f; // Multiplicador de fuerza

	[Header("Lógica de Juego")]
	[SerializeField] int totalShots = 4;     // Total de tiros iniciales
	[SerializeField] int totalEnemies = 3;   // Total de enemigos en la escena
	[SerializeField] int bonusPerShot = 500; // Puntos de bonus por tiro restante

	[Header("UI - Textos de Juego")]
	public TextMeshProUGUI textoPuntos; // Texto para mostrar puntos
	public TextMeshProUGUI textoTiros;  // Texto para mostrar tiros restantes

	[Header("UI - Paneles Finales")]
	public GameObject panelGameOver; // Panel de Game Over
	public GameObject panelWin;      // Panel de Victoria

	[Header("UI - Textos del Panel de Victoria")]
	public TextMeshProUGUI textoPuntosFinales; // Texto final de puntos
	public TextMeshProUGUI textoBonusTiros;    // Texto final de bonus
	public TextMeshProUGUI textoTotal;         // Texto final de puntuación total

	// --- Variables Privadas ---
	private Camera cam;
	private Ball currentBall;       // La bola actualmente en juego

	private int remainingShots;     // Contador de tiros restantes
	private int enemiesRemaining;   // Contador de enemigos restantes
	private int currentScore = 0;   // Puntuación actual

	private bool isDragging = false;
	private bool isTurnActive = false;  // true = la bola está en el aire
	private bool gameIsOver = false;    // true = el juego ha terminado (Win/Lose)

	// Variables de Drag
	private Vector2 startPoint, endPoint, direction, force;
	private float distance;

	//---------------------------------------

	void Start()
	{
		cam = Camera.main;

		// Inicializar contadores
		remainingShots = totalShots;
		enemiesRemaining = totalEnemies;
		currentScore = 0;
		gameIsOver = false;

		// Asegurarse de que los paneles finales estén ocultos
		panelGameOver.SetActive(false);
		panelWin.SetActive(false);

		// Actualizar UI inicial
		UpdateScoreUI();
		UpdateShotsUI();

		// Empezar juego
		SpawnNewBall();
	}

	void Update()
	{
		// Si el juego terminó (mostrando panel Win/Lose), no hacer nada
		if (gameIsOver) return;

		// 1. LÓGICA DE INPUT (DRAG)
		// Solo podemos arrastrar si hay una bola y el turno NO está activo
		if (currentBall != null && !isTurnActive)
		{
			if (Input.GetMouseButtonDown(0))
			{
				isDragging = true;
				OnDragStart();
			}
			if (Input.GetMouseButtonUp(0))
			{
				isDragging = false;
				OnDragEnd();
			}
			if (isDragging)
			{
				OnDrag();
			}
		}

		// 2. LÓGICA DE FIN DE TURNO (BOLA PARADA)
		// Si el turno está activo Y la bola existe Y ya no se mueve...
		if (isTurnActive && currentBall != null && !currentBall.isMoving)
		{
			IniciarProcesoEndTurn();
		}
	}

	/// <summary>
	/// Llamado por Ball.cs si se cae del mapa (zona "Respawn")
	/// </summary>
	public void ForzarFinDeTurno()
	{
		if (gameIsOver) return;
		IniciarProcesoEndTurn();
	}

	/// <summary>
	/// Función central que evita llamadas duplicadas a EndTurn.
	/// </summary>
	private void IniciarProcesoEndTurn()
	{
		// Si el turno ya no está activo, ya estamos procesando el fin
		if (!isTurnActive) return;

		isTurnActive = false; // Marcamos que ya estamos procesando

		// Esperamos 1 segundo antes de calcular el resultado
		// (da tiempo a que los enemigos caigan y cuenten)
		Invoke(nameof(EndTurn), 1f);
	}

	/// <summary>
	/// Procesa el resultado del turno: comprueba victoria/derrota y resta tiros.
	/// </summary>
	void EndTurn()
	{
		// 1. Restar tiro SIEMPRE
		remainingShots--;
		Debug.Log("Turno terminado. Tiros restantes: " + remainingShots);
		UpdateShotsUI();

		// 2. Comprobar condición de VICTORIA
		if (enemiesRemaining <= 0)
		{
			HandleWin();
			return; // Salimos, el juego terminó
		}

		// 3. Comprobar condición de GAME OVER
		// (Si no ganaste Y los tiros llegaron a 0)
		if (remainingShots <= 0)
		{
			HandleGameOver();
			return; // Salimos, el juego terminó
		}

		// 4. Si el juego continúa, spawnear nueva bola
		SpawnNewBall();
	}

	/// <summary>
	/// Llamado por Cup.cs cuando un enemigo es derribado.
	/// </summary>
	public void EnemigoDerribado(int puntosGanados)
	{
		// Simplemente resta enemigos y suma puntos
		enemiesRemaining--;
		currentScore += puntosGanados;

		Debug.Log("Enemigo derribado. Quedan: " + enemiesRemaining);
		UpdateScoreUI();
	}

	/// <summary>
	/// Crea una nueva bola en el punto de spawn.
	/// </summary>
	void SpawnNewBall()
	{
		// Destruimos la bola anterior si aún existe
		if (currentBall != null)
		{
			Destroy(currentBall.gameObject, 1f);
		}

		// Crea una nueva bola desde el Prefab
		GameObject ballGO = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);
		currentBall = ballGO.GetComponent<Ball>();
		currentBall.DesactivateRb(); // La pone kinemática

		isTurnActive = false; // Esperando lanzamiento
	}

	// --- FUNCIONES DE UI Y FIN DE JUEGO ---

	void HandleWin()
	{
		Debug.Log("¡VICTORIA!");
		gameIsOver = true;

		if (currentBall != null) Destroy(currentBall.gameObject);

		// Calcular Bonus
		int bonus = remainingShots * bonusPerShot;
		int total = currentScore + bonus;

		// Actualizar textos del panel de victoria
		textoPuntosFinales.text = "Puntos: " + currentScore.ToString();
		textoBonusTiros.text = "Bonus: " + bonus.ToString();
		textoTotal.text = "Total: " + total.ToString();

		// Mostrar panel
		panelWin.SetActive(true);
	}

	void HandleGameOver()
	{
		Debug.Log("¡Sin tiros restantes! GAME OVER.");
		gameIsOver = true;

		if (currentBall != null) Destroy(currentBall.gameObject);

		// Mostrar panel
		panelGameOver.SetActive(true);
	}

	/// <summary>
	/// Función pública para un botón de reinicio en los paneles finales.
	/// </summary>
	public void ReiniciarJuego()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	void UpdateScoreUI()
	{
		if (textoPuntos != null)
		{
			textoPuntos.text = "Puntos: " + currentScore.ToString();
		}
	}

	void UpdateShotsUI()
	{
		if (textoTiros != null)
		{
			textoTiros.text = "Bolas: " + remainingShots.ToString();
		}
	}

	// --- MÉTODOS DE DRAG ---

	void OnDragStart()
	{
		currentBall.DesactivateRb();
		startPoint = cam.ScreenToWorldPoint(Input.mousePosition);
		trajectory.Show();
	}

	void OnDrag()
	{
		endPoint = cam.ScreenToWorldPoint(Input.mousePosition);
		distance = Vector2.Distance(startPoint, endPoint);
		direction = (startPoint - endPoint).normalized;
		force = direction * distance * pushForce;
		trajectory.UpdateDots(currentBall.pos, force);
	}

	void OnDragEnd()
	{
		currentBall.ActivateRb();
		currentBall.Push(force);
		trajectory.Hide();

		isTurnActive = true; // ¡El turno ha comenzado!
	}
}