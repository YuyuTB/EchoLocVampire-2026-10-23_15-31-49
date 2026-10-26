using UnityEngine;

public class PlayerNoiseEmitter : MonoBehaviour
{
    [Header("Paramètres du Spam de Canne")]
    [SerializeField] private float caneSpamResetTime = 2.0f;

    private float lastCaneTime = -10f;
    private float caneSpamAccumulator = 0f;

    // Détermine le bruit selon l'état du joueur
    public void EmitMovementNoise(bool isSneaking, bool isSprinting, bool isJumping)
    {
        // Ignore les bruits de mouvement si aucun vampire n'a fini de spawner
        if (!AreAnyVampiresActive()) return;

        float noiseValue = 0f;

        if (isJumping)
        {
            noiseValue = isSneaking ? 1.5f : 3.0f;
        }
        else if (isSprinting)
        {
            noiseValue = 2.0f;
        }
        else if (isSneaking)
        {
            noiseValue = 0.5f;
        }
        else // Marche normale
        {
            noiseValue = 1.0f;
        }

        SendNoiseToEnemies(noiseValue);
    }

    // Appelée lorsque le joueur tape avec sa canne
    public void UseCane()
    {
        // Si aucun vampire n'a fini de spawner, la canne fait du bruit normalement mais pas de spam global
        if (!AreAnyVampiresActive())
        {
            SendNoiseToEnemies(5.0f);
            return;
        }

        float noiseValue = 5.0f;

        // Si réutilisé dans la fenêtre de 2 secondes
        if (Time.time - lastCaneTime <= caneSpamResetTime)
        {
            noiseValue += 2.5f; // Bruit de ce coup précis
            caneSpamAccumulator += 5.0f + 2.5f; // Accumulation du spam
        }
        else
        {
            caneSpamAccumulator = 5.0f; // Réinitialisation de la jauge
        }

        lastCaneTime = Time.time;

        // Détection globale si le spam atteint 10
        if (caneSpamAccumulator >= 10.0f)
        {
            NotifyAllEnemiesGlobalLocation();
            caneSpamAccumulator = 0f; // Reset après alerte
        }
        else
        {
            SendNoiseToEnemies(noiseValue);
        }
    }

    private void SendNoiseToEnemies(float noiseAmount)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            var vampire = enemy.GetComponent<VampireAnimationController>();
            if (vampire != null)
            {
                vampire.ReceiveNoise(transform.position, noiseAmount);
            }
        }
    }

    private void NotifyAllEnemiesGlobalLocation()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            var vampire = enemy.GetComponent<VampireAnimationController>();
            if (vampire != null)
            {
                vampire.TriggerGlobalDetection(transform.position);
            }
        }
    }

    // Vérifie s'il y a au moins un vampire actif (qui a fini son attente de spawn)
    private bool AreAnyVampiresActive()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return false;

        foreach (GameObject enemy in enemies)
        {
            var vampire = enemy.GetComponent<VampireAnimationController>();
            if (vampire != null)
            {
                // On s'assure de pouvoir lire l'état ou on vérifie si le vampire n'est plus en SpawnWait.
                // Pour que cela fonctionne, rendez l'énumérateur ou une méthode publique dans VampireAnimationController, 
                // ou vérifiez simplement si le script du vampire a une méthode/propriété pour ça.

                // Astuce : Comme l'état est privé, on peut ajouter une simple méthode publique 
                // "public bool IsSpawned() { return currentState != AIState.SpawnWait; }" dans le script du vampire.
                if (vampire.IsSpawned())
                {
                    return true; // Dès qu'un vampire est réveillé, le système s'active
                }
            }
        }
        return false;
    }
}