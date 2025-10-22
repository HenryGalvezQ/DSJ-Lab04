using UnityEngine;

public class Cup : MonoBehaviour
{
    private bool haSidoDerribado = false;

    // NUEVO: Puntos que da este enemigo
    [SerializeField] int puntosAlMorir = 500;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (this.CompareTag("Enemigo") && other.CompareTag("Suelo") && !haSidoDerribado)
        {
            haSidoDerribado = true;

            // MODIFICADO: Llamamos a la nueva función
            GameManager.Instance.EnemigoDerribado(puntosAlMorir);

            Destroy(gameObject, 0.5f);
        }
    }
}