using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine.Networking;

/// <summary>
/// Handles communication with the THEODEN leaderboard API.
/// </summary>
public sealed class LeaderboardApiClient
{
    private const int RequestTimeoutSeconds = 10;

    private readonly string baseUrl;

    public LeaderboardApiClient(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException(
                "The leaderboard base URL cannot be empty.",
                nameof(baseUrl)
            );
        }

        this.baseUrl =
            baseUrl.Trim().TrimEnd('/');
    }

    /// <summary>
    /// Creates or updates the current player's result.
    /// </summary>
    public IEnumerator SubmitResult(
        LeaderboardSubmissionDTO submission,
        Action<LeaderboardEntryDTO> onSuccess,
        Action<string> onError)
    {
        if (submission == null)
        {
            onError?.Invoke(
                "The leaderboard submission is null."
            );

            yield break;
        }

        string requestUrl =
            baseUrl + "/leaderboard/submit";

        string json =
            JsonConvert.SerializeObject(submission);

        byte[] requestBody =
            Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request =
               new UnityWebRequest(
                   requestUrl,
                   UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler =
                new UploadHandlerRaw(requestBody);

            request.downloadHandler =
                new DownloadHandlerBuffer();

            request.timeout =
                RequestTimeoutSeconds;

            request.SetRequestHeader(
                "Content-Type",
                "application/json"
            );

            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    CreateRequestError(request)
                );

                yield break;
            }

            try
            {
                LeaderboardEntryDTO result =
                    JsonConvert.DeserializeObject<
                        LeaderboardEntryDTO>(
                        request.downloadHandler.text
                    );

                if (result == null)
                {
                    onError?.Invoke(
                        "The leaderboard service returned " +
                        "an empty result."
                    );

                    yield break;
                }

                onSuccess?.Invoke(result);
            }
            catch (JsonException exception)
            {
                onError?.Invoke(
                    "Could not read the leaderboard response: " +
                    exception.Message
                );
            }
        }
    }

    /// <summary>
    /// Gets the ordered leaderboard of one THEODEN project.
    /// </summary>
    public IEnumerator GetLeaderboard(
        string projectId,
        Action<List<LeaderboardEntryDTO>> onSuccess,
        Action<string> onError)
    {
        if (string.IsNullOrWhiteSpace(projectId))
        {
            onError?.Invoke(
                "The project identifier is empty."
            );

            yield break;
        }

        string encodedProjectId =
            UnityWebRequest.EscapeURL(
                projectId.Trim()
            );

        string requestUrl =
            baseUrl +
            "/leaderboard?project_id=" +
            encodedProjectId;

        using (UnityWebRequest request =
               UnityWebRequest.Get(requestUrl))
        {
            request.timeout =
                RequestTimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result !=
                UnityWebRequest.Result.Success)
            {
                onError?.Invoke(
                    CreateRequestError(request)
                );

                yield break;
            }

            try
            {
                List<LeaderboardEntryDTO> results =
                    JsonConvert.DeserializeObject<
                        List<LeaderboardEntryDTO>>(
                        request.downloadHandler.text
                    );

                onSuccess?.Invoke(
                    results ??
                    new List<LeaderboardEntryDTO>()
                );
            }
            catch (JsonException exception)
            {
                onError?.Invoke(
                    "Could not read the leaderboard response: " +
                    exception.Message
                );
            }
        }
    }

    private static string CreateRequestError(
        UnityWebRequest request)
    {
        string message =
            $"Leaderboard request failed. " +
            $"HTTP status: {request.responseCode}. " +
            $"Error: {request.error}.";

        string responseBody =
            request.downloadHandler?.text;

        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            message +=
                " Server response: " +
                responseBody;
        }

        return message;
    }
}