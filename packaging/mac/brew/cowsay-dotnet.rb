# frozen_string_literal: true

# Homebrew formula for cowsay-dotnet
# Install: brew install cowsay-dotnet/tap/cowsay-dotnet
# Or from a local tap: brew install --build-from-source ./cowsay-dotnet.rb

class CowsayDotnet < Formula
  desc "A .NET reimplementation of the classic cowsay/cowthink utility"
  homepage "https://github.com/cowsay-dotnet/cowsay-dotnet"
  license "GPL-3.0-or-later"
  version "1.0.0"

  # Architecture-specific bottles/tarballs
  if Hardware::CPU.arm?
    url "https://github.com/cowsay-dotnet/cowsay-dotnet/releases/download/v#{version}/cowsay-dotnet-#{version}-osx-arm64.tar.gz"
    sha256 "PLACEHOLDER_SHA256_OSX_ARM64"
  else
    url "https://github.com/cowsay-dotnet/cowsay-dotnet/releases/download/v#{version}/cowsay-dotnet-#{version}-osx-x64.tar.gz"
    sha256 "PLACEHOLDER_SHA256_OSX_X64"
  end

  def install
    bin.install "cowsay"
    bin.install "cowthink"

    # Install cow art files
    (share / "cowsay-dotnet" / "cows").install Dir["assets/cows/*.cow"]
  end

  def caveats
    <<~EOS
      Cow art files have been installed to:
        #{share}/cowsay-dotnet/cows/

      Set COWPATH to use a custom cow directory:
        export COWPATH=#{share}/cowsay-dotnet/cows
    EOS
  end

  test do
    assert_match "moo", shell_output("#{bin}/cowsay moo")
    assert_match "moo", shell_output("#{bin}/cowthink moo")
  end
end
