FROM microsoft/dotnet:2.0-sdk as build

# Set the current working directory
WORKDIR /build

# Inject the Opux2 source into the image
COPY . /build/

# Install any needed utilities for building
RUN apt-get update && \
    apt-get install -y rsync=3.1.2-1+deb9u1

# Build Opux2
RUN ./build_linux.sh

FROM microsoft/dotnet:2.0-runtime

LABEL maintainer="guy.pascarella@gmail.com"

# Set the current working directory
WORKDIR /app

# Copy from the first image into here
COPY --from=build /build/Release .

# Note: The run command of dotnet is the more official version of the exec command
#CMD ["dotnet", "run"]
#CMD ["dotnet", "exec", "/app/bin/Debug/netcoreapp2.0/Opux.dll"]
#CMD ["dotnet", "exec", "/app/Opux2.dll"]
CMD ["./Opux2"]
