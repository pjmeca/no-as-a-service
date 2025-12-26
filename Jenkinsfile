#!groovy

pipeline {
    agent any

    parameters {
        string(
            name: 'HOST_PORT',
            defaultValue: '31325',
            description: 'Host port to expose the container'
        )
    }

    environment {
        IMAGE_NAME = 'pjmeca/no-as-a-service'
        CONTAINER_NAME = 'no-as-a-service'
    }

    stages {

        stage('Docker Build') {
            steps {
                script {
                    env.TS = sh(
                        script: "date +%Y%m%d-%H%M%S",
                        returnStdout: true
                    ).trim()

                    sh """
                        docker build \
                          -t ${IMAGE_NAME}:${env.TS} \
                          -t ${IMAGE_NAME}:latest .
                    """
                }
            }
        }

        stage('Docker Deploy') {
            steps {
                sh """
                    # Stop and remove previous container if it exists
                    docker rm -f ${CONTAINER_NAME} || true

                    # Run new container
                    docker run -d \
                      --name ${CONTAINER_NAME} \
                      -p ${params.HOST_PORT}:5000 \
                      --restart unless-stopped \
                      ${IMAGE_NAME}:latest
                """
            }
        }
    }
}
