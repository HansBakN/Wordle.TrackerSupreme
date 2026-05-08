pipeline {
    agent any

    parameters {
        string(name: 'IMAGE_TAG', description: 'Docker image tag to promote')
        choice(name: 'TARGET_ENV', choices: ['QA', 'PROD'], description: 'Deployment target')
    }

    environment {
        REGISTRY = "registry.trackersupreme.dk"
    }

    stages {
        stage('Validate Input') {
            steps {
                script {
                    if (!params.IMAGE_TAG?.trim()) {
                        error("IMAGE_TAG is required")
                    }

                    env.TARGET_TAG = params.TARGET_ENV == 'QA' ? 'qa-latest' : 'latest'

                    echo "Promoting images:"
                    echo "  From: ${params.IMAGE_TAG}"
                    echo "  To:   ${env.TARGET_TAG}"
                }
                script {
                    IMAGES = [
                        'web',
                        'api',
                        'migrator'
                    ]
                }
            }
        }

        stage('Validate Images Exist') {
            steps {
                script {
                    for (img in IMAGES) {
                        IMAGE_PATH = "${REGISTRY}/wordle-trackersupreme-${img}:${params.IMAGE_TAG}"
                        echo "Validating image: ${IMAGE_PATH}"
                        sh """
                            docker manifest inspect \
                            ${IMAGE_PATH} \
                            > /dev/null
                        """
                    }
                }
            }
        }

        stage('Pull Images') {
            steps {
                script {
                    for (img in IMAGES) {
                        IMAGE_PATH = "${REGISTRY}/wordle-trackersupreme-${img}"
                        sh """
                            docker pull ${IMAGE_PATH}:${params.IMAGE_TAG}
                        """
                    }
                }
            }
        }

        stage('Tag Images') {
            steps {
                script {
                    for (img in IMAGES) {
                        IMAGE_PATH = "${REGISTRY}/wordle-trackersupreme-${img}"
                        sh """
                            docker tag ${IMAGE_PATH}:${params.IMAGE_TAG} ${IMAGE_PATH}:${TARGET_TAG}
                        """
                    }
                }
            }
        }

        stage('Push Promoted Tags') {
            steps {
                script {
                    for (img in IMAGES) {
                        IMAGE_PATH = "${REGISTRY}/wordle-trackersupreme-${img}"
                        sh """
                            docker push ${IMAGE_PATH}:${TARGET_TAG}
                        """
                    }
                }
            }
        }
    }

    post {
        success {
            echo "Promotion completed"
        }
    }
}