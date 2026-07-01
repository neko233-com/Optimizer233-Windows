const releaseTag = document.getElementById("release-tag");
const releaseAsset = document.getElementById("release-asset");
const latestDownload = document.getElementById("latest-download");

async function loadRelease() {
  try {
    const response = await fetch("https://api.github.com/repos/neko233-com/Optimizer233-Windows/releases/latest", {
      headers: {
        Accept: "application/vnd.github+json"
      }
    });

    if (!response.ok) {
      throw new Error(`GitHub API ${response.status}`);
    }

    const release = await response.json();
    const asset = (release.assets || []).find(item => item.name.endsWith("-win11-x64.zip"));

    releaseTag.textContent = release.tag_name || "latest";
    releaseAsset.textContent = asset ? `${asset.name} · ${(asset.size / 1024 / 1024).toFixed(1)} MB` : "No release asset found";

    if (asset?.browser_download_url) {
      latestDownload.href = asset.browser_download_url;
    }
  } catch (error) {
    releaseTag.textContent = "unavailable";
    releaseAsset.textContent = "Latest release metadata unavailable";
  }
}

loadRelease();
