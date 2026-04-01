%global package_name cowsay-dotnet
%global version_num %{?_version}%{!?_version:1.0.0}

Name:           %{package_name}
Version:        %{version_num}
Release:        1%{?dist}
Summary:        A .NET reimplementation of the classic cowsay/cowthink utility

License:        GPL-3.0-or-later
URL:            https://github.com/cowsay-dotnet/cowsay-dotnet
Source0:        %{package_name}-%{version_num}.tar.gz

# Self-contained binary — no runtime dependencies
AutoReqProv:    no

%description
cowsay-dotnet is a faithful .NET reimplementation of the classic Unix
cowsay and cowthink programs. It generates ASCII art of a cow (or other
characters) saying or thinking a given message.

This package provides the cowsay and cowthink command-line tools as
self-contained binaries with no external runtime dependencies.

%prep
# Nothing to prepare — we use pre-built binaries

%build
# Nothing to build — binaries are pre-compiled

%install
rm -rf %{buildroot}

# Install binaries
install -D -m 755 %{_sourcedir}/cowsay   %{buildroot}%{_bindir}/cowsay
install -D -m 755 %{_sourcedir}/cowthink %{buildroot}%{_bindir}/cowthink

# Install cow files
mkdir -p %{buildroot}%{_datadir}/%{package_name}/cows
cp -r %{_sourcedir}/assets/cows/* %{buildroot}%{_datadir}/%{package_name}/cows/

# Install license
install -D -m 644 %{_sourcedir}/LICENSE %{buildroot}%{_docdir}/%{package_name}/LICENSE

%files
%license %{_docdir}/%{package_name}/LICENSE
%{_bindir}/cowsay
%{_bindir}/cowthink
%{_datadir}/%{package_name}/cows/

%changelog
* %(date "+%a %b %d %Y") cowsay-dotnet contributors <cowsay-dotnet@users.noreply.github.com> - %{version_num}-1
- Initial package release
